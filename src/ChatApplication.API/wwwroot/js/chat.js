// ── Auth guard ────────────────────────────────────────────
const token    = localStorage.getItem('token');
const myUserId = localStorage.getItem('userId');
const myName   = localStorage.getItem('username');

if (!token) { location.href = '/'; }

// ── State ─────────────────────────────────────────────────
let hub           = null;
let currentRoomId = null;
let typingTimer   = null;
let typingUsers   = {};          // userId → username
let lastMsgAuthor = null;        // for grouping consecutive messages
let unreadCounts  = {};          // roomId → count

// ── API ───────────────────────────────────────────────────
async function api(path, opts = {}) {
  const res = await fetch(path, {
    ...opts,
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}`, ...(opts.headers || {}) }
  });
  if (res.status === 401) { logout(); return null; }
  if (res.status === 204) return null;
  return res.json();
}

// ── Boot ──────────────────────────────────────────────────
async function init() {
  renderSelf();
  await loadRooms();
  await connectHub();
  await loadOnlineUsers();
}

function renderSelf() {
  document.getElementById('user-avatar').textContent = (myName || '?')[0].toUpperCase();
  document.getElementById('user-avatar').style.position = 'relative';
  document.getElementById('user-name').textContent  = myName || '—';
  document.getElementById('user-email').textContent = localStorage.getItem('email') || '';
}

// ── Rooms ─────────────────────────────────────────────────
async function loadRooms() {
  const data = await api('/api/chat/rooms');
  if (!data) return;
  renderRooms(data.data || []);
}

function renderRooms(rooms) {
  const list = document.getElementById('room-list');
  if (!rooms.length) {
    list.innerHTML = `<div style="padding:.75rem .5rem;font-size:.8rem;color:var(--text-muted)">No rooms yet — create one!</div>`;
    return;
  }
  list.innerHTML = rooms.map(r => {
    const unread = unreadCounts[r.id] || 0;
    return `
      <div class="room-item ${r.id === currentRoomId ? 'active' : ''}" id="room-${r.id}"
           onclick="selectRoom('${r.id}','${escHtml(r.name)}')">
        <span class="room-hash">#</span>
        <span style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escHtml(r.name)}</span>
        ${unread ? `<span class="room-unread">${unread}</span>` : ''}
      </div>`;
  }).join('');
}

async function selectRoom(roomId, roomName) {
  if (currentRoomId === roomId) return;

  // Leave previous SignalR group
  if (currentRoomId && hub) {
    hub.invoke('LeaveRoom', currentRoomId).catch(() => {});
  }

  currentRoomId = roomId;
  lastMsgAuthor = null;
  unreadCounts[roomId] = 0;

  // Update sidebar
  document.querySelectorAll('.room-item').forEach(el => el.classList.remove('active'));
  document.getElementById(`room-${roomId}`)?.classList.add('active');

  // Show chat area
  document.getElementById('no-room-state').style.display = 'none';
  const ar = document.getElementById('active-room');
  ar.style.display = 'flex';

  document.getElementById('active-room-name').textContent = roomName;
  document.getElementById('active-room-meta').textContent = '';
  document.getElementById('msg-input').placeholder = `Message #${roomName}…`;
  document.getElementById('msg-input').focus();

  clearMessages();
  await loadMessages(roomId);

  if (hub) hub.invoke('JoinRoom', roomId).catch(() => {});
}

async function loadMessages(roomId) {
  if (!roomId) return;
  const data = await api(`/api/chat/rooms/${roomId}/messages?page=1&pageSize=60`);
  if (!data?.data?.items) return;
  clearMessages();
  lastMsgAuthor = null;
  data.data.items.forEach(m => appendMessage(m, false));
  scrollToBottom(false);
}

function clearMessages() {
  document.getElementById('messages-area').innerHTML = '';
  lastMsgAuthor = null;
}

// ── Send ──────────────────────────────────────────────────
async function sendMessage() {
  const input   = document.getElementById('msg-input');
  const content = input.value.trim();
  if (!content || !currentRoomId) return;

  input.value = '';
  stopTyping();

  if (hub?.state === 'Connected') {
    hub.invoke('SendMessage', currentRoomId, content).catch(async err => {
      toast('Failed to send: ' + err.message, 'error');
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
  if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); }
}

// ── Typing ────────────────────────────────────────────────
function handleTyping() {
  if (!hub || !currentRoomId) return;
  hub.invoke('TypingInRoom', currentRoomId).catch(() => {});
  clearTimeout(typingTimer);
  typingTimer = setTimeout(stopTyping, 2500);
}

function stopTyping() {
  clearTimeout(typingTimer);
  if (!hub || !currentRoomId) return;
  hub.invoke('StoppedTypingInRoom', currentRoomId).catch(() => {});
}

function updateTypingBar() {
  const bar   = document.getElementById('typing-bar');
  const names = Object.values(typingUsers);
  if (!names.length) { bar.innerHTML = ''; return; }
  const label = names.length === 1
    ? `<strong>${escHtml(names[0])}</strong> is typing`
    : `<strong>${names.length} people</strong> are typing`;
  bar.innerHTML = `${label} <span class="typing-dots"><span></span><span></span><span></span></span>`;
}

// ── Render messages ───────────────────────────────────────
function appendMessage(msg, scroll = true) {
  const area  = document.getElementById('messages-area');
  const isMe  = msg.senderId === myUserId;
  const time  = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  const init  = (msg.senderUsername || '?')[0].toUpperCase();
  const cont  = lastMsgAuthor === msg.senderId;

  lastMsgAuthor = msg.senderId;

  const div = document.createElement('div');
  div.className = `msg-group${cont ? ' continued' : ''}`;
  div.id = `msg-${msg.id}`;
  div.innerHTML = `
    <div class="avatar xs" style="margin-top:.2rem">${escHtml(init)}</div>
    <div class="msg-body">
      <div class="msg-meta">
        <span class="msg-author${isMe ? ' me' : ''}">${escHtml(msg.senderUsername)}</span>
        <span class="msg-time">${time}</span>
      </div>
      <div class="msg-text">${escHtml(msg.content)}</div>
    </div>`;

  area.appendChild(div);
  if (scroll) scrollToBottom(true);
}

function appendSystem(text) {
  const area = document.getElementById('messages-area');
  const div  = document.createElement('div');
  div.className = 'system-msg';
  div.textContent = text;
  area.appendChild(div);
  scrollToBottom(true);
  lastMsgAuthor = null;
}

function scrollToBottom(smooth = true) {
  const area = document.getElementById('messages-area');
  area.scrollTo({ top: area.scrollHeight, behavior: smooth ? 'smooth' : 'instant' });
}

// ── Rooms CRUD ────────────────────────────────────────────
function openNewRoomModal() {
  document.getElementById('new-room-modal').classList.add('open');
  document.getElementById('new-room-name').value = '';
  document.getElementById('room-error').classList.remove('show');
  setTimeout(() => document.getElementById('new-room-name').focus(), 60);
}

function closeNewRoomModal() {
  document.getElementById('new-room-modal').classList.remove('open');
}

async function createRoom() {
  const name = document.getElementById('new-room-name').value.trim();
  if (!name) return;

  const data = await api('/api/chat/rooms', { method: 'POST', body: JSON.stringify({ name }) });
  if (!data) return;

  if (!data.success) {
    const err = document.getElementById('room-error');
    err.textContent = data.message || 'Failed to create room.';
    err.classList.add('show');
    return;
  }

  closeNewRoomModal();
  toast(`Room #${name} created!`, 'success');
  await loadRooms();
  selectRoom(data.data.id, data.data.name);
}

async function leaveCurrentRoom() {
  if (!currentRoomId) return;
  await api(`/api/chat/rooms/${currentRoomId}/leave`, { method: 'POST' });
  if (hub) hub.invoke('LeaveRoom', currentRoomId).catch(() => {});
  currentRoomId = null;
  document.getElementById('no-room-state').style.display = '';
  document.getElementById('active-room').style.display   = 'none';
  await loadRooms();
  toast('Left the room', 'info');
}

// ── Online users ──────────────────────────────────────────
async function loadOnlineUsers() {
  const data = await api('/api/user/online');
  if (!data?.data) return;
  renderOnlineUsers(data.data);
}

function renderOnlineUsers(users) {
  document.getElementById('online-count').textContent = users.length;
  const list = document.getElementById('online-list');
  if (!users.length) {
    list.innerHTML = `<div style="padding:.5rem;font-size:.78rem;color:var(--text-muted)">No one online yet</div>`;
    return;
  }
  list.innerHTML = users.map(u => {
    const init   = (u.username || '?')[0].toUpperCase();
    const status = (u.status || 'online').toLowerCase();
    return `
      <div class="online-user">
        <div class="avatar xs">${escHtml(init)}
          <span class="status-badge ${status}"></span>
        </div>
        <span class="name">${escHtml(u.username)}</span>
        <span class="status-text">${status}</span>
      </div>`;
  }).join('');
}

// ── SignalR ───────────────────────────────────────────────
async function connectHub() {
  hub = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chat', { accessTokenFactory: () => token })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  // Room message
  hub.on('ReceiveMessage', msg => {
    if (msg.roomId === currentRoomId) {
      appendMessage(msg, true);
    } else {
      // Increment unread badge
      unreadCounts[msg.roomId] = (unreadCounts[msg.roomId] || 0) + 1;
      loadRooms();
      toast(`New message in #${msg.roomId.slice(0,6)}…`, 'info');
    }
  });

  // Private message
  hub.on('ReceivePrivateMessage', msg => {
    toast(`💬 ${msg.senderUsername}: ${msg.content.slice(0, 60)}`, 'info');
  });

  // Join / leave
  hub.on('UserJoinedRoom', ({ userId, roomId }) => {
    if (roomId === currentRoomId && userId !== myUserId)
      appendSystem('Someone joined the room');
  });

  hub.on('UserLeftRoom', ({ userId, roomId }) => {
    if (roomId === currentRoomId && userId !== myUserId)
      appendSystem('Someone left the room');
  });

  // Presence
  hub.on('UserOnline',  () => loadOnlineUsers());
  hub.on('UserOffline', () => loadOnlineUsers());

  // Typing
  hub.on('UserTyping', ({ userId, roomId }) => {
    if (roomId !== currentRoomId || userId === myUserId) return;
    typingUsers[userId] = userId;
    updateTypingBar();
  });

  hub.on('UserStoppedTyping', ({ userId }) => {
    delete typingUsers[userId];
    updateTypingBar();
  });

  hub.onreconnecting(() => toast('Reconnecting…', 'info'));
  hub.onreconnected(() => {
    toast('Connected', 'success');
    if (currentRoomId) hub.invoke('JoinRoom', currentRoomId).catch(() => {});
    loadOnlineUsers();
  });
  hub.onclose(() => toast('Disconnected', 'error'));

  try {
    await hub.start();
    toast('Connected', 'success');
  } catch {
    toast('Real-time unavailable — using REST fallback', 'info');
  }
}

// ── Logout ────────────────────────────────────────────────
function logout() {
  if (hub) hub.stop().catch(() => {});
  localStorage.clear();
  location.href = '/';
}

// ── Toast ─────────────────────────────────────────────────
const _toastTimers = {};
function toast(msg, type = 'info') {
  const container = document.getElementById('toast-container');
  const id  = Date.now();
  const el  = document.createElement('div');
  const icon = type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ';
  el.className = `toast ${type}`;
  el.innerHTML = `<span class="toast-icon">${icon}</span><span>${escHtml(msg)}</span>`;
  container.appendChild(el);
  requestAnimationFrame(() => el.classList.add('show'));
  _toastTimers[id] = setTimeout(() => {
    el.classList.remove('show');
    setTimeout(() => el.remove(), 350);
  }, 3500);
}

// ── Helpers ───────────────────────────────────────────────
function escHtml(s) {
  return String(s ?? '')
    .replace(/&/g,'&amp;').replace(/</g,'&lt;')
    .replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// Modal keyboard
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') closeNewRoomModal();
});
document.getElementById('new-room-modal').addEventListener('click', e => {
  if (e.target === e.currentTarget) closeNewRoomModal();
});

// ── Start ─────────────────────────────────────────────────
init();
