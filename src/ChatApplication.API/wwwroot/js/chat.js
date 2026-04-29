// ── Auth guard ────────────────────────────────────────────
const token    = localStorage.getItem('token');
const userId   = localStorage.getItem('userId');
const username = localStorage.getItem('username');

if (!token) { window.location.href = '/'; }

// ── State ─────────────────────────────────────────────────
let connection      = null;
let currentRoomId   = null;
let currentRoomName = null;
let typingTimer     = null;
let typingUsers     = new Set();

// ── API helper ────────────────────────────────────────────
async function api(path, options = {}) {
  const res = await fetch(path, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
      ...(options.headers || {})
    }
  });
  if (res.status === 401) { logout(); return null; }
  if (res.status === 204)  return null;
  return res.json();
}

// ── Init ──────────────────────────────────────────────────
async function init() {
  renderUserInfo();
  await loadRooms();
  await connectSignalR();
  await loadOnlineUsers();
}

function renderUserInfo() {
  const initial = (username || '?')[0].toUpperCase();
  document.getElementById('user-avatar').textContent = initial;
  document.getElementById('user-name').textContent   = username || '—';
  document.getElementById('user-role').textContent   = 'Member';
}

// ── Rooms ─────────────────────────────────────────────────
async function loadRooms() {
  const data = await api('/api/chat/rooms');
  if (!data) return;
  const rooms = data.data || [];
  renderRooms(rooms);
}

function renderRooms(rooms) {
  const list = document.getElementById('room-list');
  if (!rooms.length) {
    list.innerHTML = '<div class="system-msg" style="padding:.75rem">No rooms yet. Create one!</div>';
    return;
  }
  list.innerHTML = rooms.map(r => `
    <div class="room-item ${r.id === currentRoomId ? 'active' : ''}"
         id="room-${r.id}"
         onclick="selectRoom('${r.id}', '${escHtml(r.name)}')">
      <span class="hash">#</span>${escHtml(r.name)}
    </div>
  `).join('');
}

async function selectRoom(roomId, roomName) {
  if (currentRoomId === roomId) return;

  // Leave previous SignalR group
  if (currentRoomId && connection) {
    await connection.invoke('LeaveRoom', currentRoomId).catch(() => {});
  }

  currentRoomId   = roomId;
  currentRoomName = roomName;

  // Update UI
  document.querySelectorAll('.room-item').forEach(el => el.classList.remove('active'));
  const roomEl = document.getElementById(`room-${roomId}`);
  if (roomEl) roomEl.classList.add('active');

  document.getElementById('no-room-state').style.display  = 'none';
  const activeRoom = document.getElementById('active-room');
  activeRoom.style.display = 'flex';

  document.getElementById('active-room-name').textContent = roomName;
  document.getElementById('active-room-meta').textContent = '';
  document.getElementById('msg-input').placeholder = `Message #${roomName}…`;

  clearMessages();
  await loadMessages(roomId);

  // Join SignalR group
  if (connection) {
    await connection.invoke('JoinRoom', roomId).catch(() => {});
  }
}

async function loadMessages(roomId) {
  const data = await api(`/api/chat/rooms/${roomId}/messages?page=1&pageSize=50`);
  if (!data?.data?.items) return;
  const msgs = data.data.items;
  msgs.forEach(m => appendMessage(m, false));
  scrollToBottom();
}

function clearMessages() {
  document.getElementById('messages-area').innerHTML = '';
}

// ── Send message ──────────────────────────────────────────
async function sendMessage() {
  const input = document.getElementById('msg-input');
  const content = input.value.trim();
  if (!content || !currentRoomId) return;

  input.value = '';
  stopTyping();

  if (connection?.state === 'Connected') {
    // Prefer SignalR for real-time delivery
    await connection.invoke('SendMessage', currentRoomId, content).catch(async () => {
      // Fallback to REST
      await api(`/api/chat/rooms/${currentRoomId}/messages`, {
        method: 'POST',
        body: JSON.stringify({ content })
      });
    });
  } else {
    const data = await api(`/api/chat/rooms/${currentRoomId}/messages`, {
      method: 'POST',
      body: JSON.stringify({ content })
    });
    if (data?.data) appendMessage(data.data, true);
  }
}

function handleInputKey(e) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault();
    sendMessage();
  }
}

// ── Typing indicator ──────────────────────────────────────
function handleTyping() {
  if (!connection || !currentRoomId) return;
  connection.invoke('TypingInRoom', currentRoomId).catch(() => {});
  clearTimeout(typingTimer);
  typingTimer = setTimeout(stopTyping, 2000);
}

function stopTyping() {
  if (!connection || !currentRoomId) return;
  connection.invoke('StoppedTypingInRoom', currentRoomId).catch(() => {});
}

function updateTypingIndicator() {
  const el = document.getElementById('typing-indicator');
  if (!typingUsers.size) { el.textContent = ''; return; }
  const names = [...typingUsers].join(', ');
  el.textContent = `${names} ${typingUsers.size === 1 ? 'is' : 'are'} typing…`;
}

// ── Render messages ───────────────────────────────────────
function appendMessage(msg, scroll = true) {
  const area = document.getElementById('messages-area');
  const isMe = msg.senderId === userId;
  const time  = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  const initial = (msg.senderUsername || '?')[0].toUpperCase();

  const div = document.createElement('div');
  div.className = 'msg-group';
  div.id = `msg-${msg.id}`;
  div.innerHTML = `
    <div class="avatar sm">${escHtml(initial)}</div>
    <div class="msg-body">
      <div class="msg-meta">
        <span class="msg-author ${isMe ? 'me' : ''}">${escHtml(msg.senderUsername)}</span>
        <span class="msg-time">${time}</span>
      </div>
      <div class="msg-text">${escHtml(msg.content)}</div>
    </div>
  `;
  area.appendChild(div);
  if (scroll) scrollToBottom();
}

function appendSystemMessage(text) {
  const area = document.getElementById('messages-area');
  const div = document.createElement('div');
  div.className = 'system-msg';
  div.textContent = text;
  area.appendChild(div);
  scrollToBottom();
}

function scrollToBottom() {
  const area = document.getElementById('messages-area');
  area.scrollTop = area.scrollHeight;
}

// ── Rooms CRUD ────────────────────────────────────────────
function openNewRoomModal() {
  document.getElementById('new-room-modal').classList.add('open');
  document.getElementById('new-room-name').value = '';
  document.getElementById('room-error').classList.remove('show');
  setTimeout(() => document.getElementById('new-room-name').focus(), 50);
}

function closeNewRoomModal() {
  document.getElementById('new-room-modal').classList.remove('open');
}

async function createRoom() {
  const name = document.getElementById('new-room-name').value.trim();
  if (!name) return;

  const data = await api('/api/chat/rooms', {
    method: 'POST',
    body: JSON.stringify({ name })
  });

  if (!data) return;

  if (!data.success) {
    const errEl = document.getElementById('room-error');
    errEl.textContent = data.message || 'Failed to create room.';
    errEl.classList.add('show');
    return;
  }

  closeNewRoomModal();
  showToast(`Room #${name} created!`, 'success');
  await loadRooms();
  selectRoom(data.data.id, data.data.name);
}

async function leaveCurrentRoom() {
  if (!currentRoomId) return;
  await api(`/api/chat/rooms/${currentRoomId}/leave`, { method: 'POST' });
  if (connection) await connection.invoke('LeaveRoom', currentRoomId).catch(() => {});
  currentRoomId = null;
  document.getElementById('no-room-state').style.display = '';
  document.getElementById('active-room').style.display   = 'none';
  await loadRooms();
}

// ── Online users ──────────────────────────────────────────
async function loadOnlineUsers() {
  const data = await api('/api/user/online');
  if (!data?.data) return;
  renderOnlineUsers(data.data);
}

function renderOnlineUsers(users) {
  const list  = document.getElementById('online-list');
  const count = document.getElementById('online-count');
  count.textContent = users.length;

  if (!users.length) {
    list.innerHTML = '<div class="system-msg" style="padding:.5rem">No one online</div>';
    return;
  }

  list.innerHTML = users.map(u => `
    <div class="online-user">
      <span class="status-dot ${u.status.toLowerCase()}"></span>
      <span>${escHtml(u.username)}</span>
    </div>
  `).join('');
}

// ── SignalR ───────────────────────────────────────────────
async function connectSignalR() {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chat', { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  // Incoming message
  connection.on('ReceiveMessage', msg => {
    if (msg.roomId === currentRoomId) {
      appendMessage(msg, true);
    }
  });

  // Private message
  connection.on('ReceivePrivateMessage', msg => {
    showToast(`DM from ${msg.senderUsername}: ${msg.content}`);
  });

  // User joined / left room
  connection.on('UserJoinedRoom', ({ userId: uid, roomId }) => {
    if (roomId === currentRoomId && uid !== userId) {
      appendSystemMessage(`A user joined the room.`);
    }
  });

  connection.on('UserLeftRoom', ({ userId: uid, roomId }) => {
    if (roomId === currentRoomId && uid !== userId) {
      appendSystemMessage(`A user left the room.`);
    }
  });

  // Presence
  connection.on('UserOnline', uid => {
    loadOnlineUsers();
  });

  connection.on('UserOffline', uid => {
    loadOnlineUsers();
  });

  // Typing
  connection.on('UserTyping', ({ userId: uid }) => {
    if (uid === userId) return;
    typingUsers.add(uid);
    updateTypingIndicator();
  });

  connection.on('UserStoppedTyping', ({ userId: uid }) => {
    typingUsers.delete(uid);
    updateTypingIndicator();
  });

  connection.onreconnected(() => {
    showToast('Reconnected', 'success');
    if (currentRoomId) connection.invoke('JoinRoom', currentRoomId).catch(() => {});
  });

  connection.onclose(() => showToast('Disconnected from server', 'error'));

  try {
    await connection.start();
  } catch (e) {
    showToast('Could not connect to real-time server', 'error');
  }
}

// ── Presence hub (separate connection) ───────────────────
async function connectPresenceHub() {
  const presenceConn = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/presence', { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  presenceConn.on('OnlineUsers', users => renderOnlineUsers(users));
  presenceConn.on('UserOnline',  ()    => loadOnlineUsers());
  presenceConn.on('UserOffline', ()    => loadOnlineUsers());

  try { await presenceConn.start(); } catch { /* non-critical */ }
}

// ── Logout ────────────────────────────────────────────────
function logout() {
  localStorage.clear();
  window.location.href = '/';
}

// ── Toast ─────────────────────────────────────────────────
function showToast(msg, type = '') {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.className = `toast ${type} show`;
  setTimeout(() => el.classList.remove('show'), 3000);
}

// ── Escape HTML ───────────────────────────────────────────
function escHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

// ── Modal keyboard close ──────────────────────────────────
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') closeNewRoomModal();
});

document.getElementById('new-room-modal').addEventListener('click', e => {
  if (e.target === e.currentTarget) closeNewRoomModal();
});

// ── Boot ──────────────────────────────────────────────────
init().then(() => connectPresenceHub());
