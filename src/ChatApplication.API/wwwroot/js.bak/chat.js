// ── Auth guard ────────────────────────────────────────────
const token    = localStorage.getItem('token');
const myUserId = localStorage.getItem('userId');
const myName   = localStorage.getItem('username');
if (!token) { location.href = '/'; }

// ── State ─────────────────────────────────────────────────
let hub           = null;
let currentRoomId = null;
let typingTimer   = null;
let typingUsers   = {};
let lastAuthorId  = null;   // for bubble grouping
let unreadCounts  = {};

// Edit state
let editingMsgId      = null;
let editingMsgContent = null;

// Context menu state
let ctxMsgId      = null;
let ctxMsgContent = null;
let ctxIsMe       = false;

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
  const av = document.getElementById('user-avatar');
  av.childNodes[0].textContent = (myName || '?')[0].toUpperCase();
  document.getElementById('user-name').textContent = myName || '—';
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
    const u = unreadCounts[r.id] || 0;
    return `<div class="room-item ${r.id === currentRoomId ? 'active' : ''}" id="room-${r.id}"
         onclick="selectRoom('${r.id}','${escHtml(r.name)}')">
      <span class="room-hash">#</span>
      <span style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escHtml(r.name)}</span>
      ${u ? `<span class="room-unread">${u}</span>` : ''}
    </div>`;
  }).join('');
}

async function selectRoom(roomId, roomName) {
  if (currentRoomId === roomId) return;
  if (currentRoomId && hub) hub.invoke('LeaveRoom', currentRoomId).catch(() => {});

  currentRoomId = roomId;
  lastAuthorId  = null;
  unreadCounts[roomId] = 0;
  cancelEdit();

  document.querySelectorAll('.room-item').forEach(el => el.classList.remove('active'));
  document.getElementById(`room-${roomId}`)?.classList.add('active');

  document.getElementById('no-room-state').style.display = 'none';
  const ar = document.getElementById('active-room');
  ar.style.display = 'flex';

  document.getElementById('active-room-name').textContent = roomName;
  document.getElementById('msg-input').placeholder = `Message #${roomName}…`;
  document.getElementById('msg-input').focus();

  clearMessages();
  await loadMessages(roomId);
  if (hub) hub.invoke('JoinRoom', roomId).catch(() => {});
}

async function loadMessages(roomId) {
  if (!roomId) return;
  const data = await api(`/api/chat/rooms/${roomId}/messages?page=1&pageSize=80`);
  if (!data?.data?.items) return;
  clearMessages();
  lastAuthorId = null;
  data.data.items.forEach(m => appendMessage(m, false));
  scrollToBottom(false);
}

function clearMessages() {
  document.getElementById('messages-area').innerHTML = '';
  lastAuthorId = null;
}

// ── Send / Edit ───────────────────────────────────────────
async function sendOrEdit() {
  if (editingMsgId) { await submitEdit(); }
  else              { await sendMessage(); }
}

async function sendMessage() {
  const input   = document.getElementById('msg-input');
  const content = input.value.trim();
  if (!content || !currentRoomId) return;
  input.value = '';
  stopTyping();

  if (hub?.state === 'Connected') {
    hub.invoke('SendMessage', currentRoomId, content).catch(err => toast('Send failed: ' + err.message, 'error'));
  } else {
    const data = await api(`/api/chat/rooms/${currentRoomId}/messages`, {
      method: 'POST', body: JSON.stringify({ content })
    });
    if (data?.data) appendMessage(data.data, true);
  }
}

// ── Edit ──────────────────────────────────────────────────
function startEdit(msgId, content) {
  editingMsgId      = msgId;
  editingMsgContent = content;
  const input = document.getElementById('msg-input');
  input.value = content;
  input.focus();
  document.getElementById('edit-preview').textContent = content.slice(0, 40) + (content.length > 40 ? '…' : '');
  document.getElementById('edit-banner').classList.add('show');
  closeCtxMenu();
}

function cancelEdit() {
  editingMsgId = null;
  editingMsgContent = null;
  document.getElementById('msg-input').value = '';
  document.getElementById('edit-banner').classList.remove('show');
}

async function submitEdit() {
  const input   = document.getElementById('msg-input');
  const content = input.value.trim();
  if (!content || !editingMsgId) return;

  const msgId = editingMsgId;
  cancelEdit();

  const data = await api(`/api/chat/messages/${msgId}`, {
    method: 'PUT', body: JSON.stringify({ content })
  });

  if (data?.data) {
    updateMessageInDOM(data.data);
    toast('Message edited', 'success');
  }
}

// ── Delete ────────────────────────────────────────────────
async function deleteMessage(msgId) {
  const data = await api(`/api/chat/messages/${msgId}`, { method: 'DELETE' });
  // 204 = success
  markDeletedInDOM(msgId);
  toast('Message deleted', 'info');
  closeCtxMenu();
}

// ── Input handlers ────────────────────────────────────────
function handleInputKey(e) {
  if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendOrEdit(); }
  if (e.key === 'Escape' && editingMsgId) cancelEdit();
}

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
  bar.innerHTML = `${label}&nbsp;<span class="typing-dots"><span></span><span></span><span></span></span>`;
}

// ── Render messages ───────────────────────────────────────
function appendMessage(msg, scroll = true) {
  const area  = document.getElementById('messages-area');
  const isMe  = msg.senderId === myUserId;
  const cont  = lastAuthorId === msg.senderId;
  lastAuthorId = msg.senderId;

  const time  = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  const init  = (msg.senderUsername || '?')[0].toUpperCase();

  const row = document.createElement('div');
  row.className = `msg-row ${isMe ? 'me' : 'other'}${cont ? ' continued' : ''}`;
  row.id = `msg-${msg.id}`;
  row.dataset.msgId      = msg.id;
  row.dataset.content    = msg.content;
  row.dataset.isMe       = isMe ? '1' : '0';
  row.dataset.senderId   = msg.senderId;

  const editedTag = msg.isEdited ? `<span class="edited-tag">edited</span>` : '';
  const bodyText  = msg.isDeleted
    ? `<span class="deleted-bubble">🚫 This message was deleted</span>`
    : escHtml(msg.content);

  row.innerHTML = `
    <div class="avatar sm">${escHtml(init)}</div>
    <div class="bubble-wrap">
      <div class="bubble-name">${escHtml(msg.senderUsername)}</div>
      <div class="bubble" oncontextmenu="openCtxMenu(event,'${msg.id}',this)">${bodyText}</div>
      <div class="bubble-meta">
        ${editedTag}
        <span class="bubble-time">${time}</span>
      </div>
    </div>
    <div class="msg-actions">
      ${!msg.isDeleted && isMe ? `
        <button class="action-btn" onclick="startEdit('${msg.id}', document.getElementById('msg-${msg.id}').dataset.content)" title="Edit">✏️</button>
      ` : ''}
      <button class="action-btn" onclick="copyText(document.getElementById('msg-${msg.id}').dataset.content)" title="Copy">📋</button>
      ${!msg.isDeleted && isMe ? `
        <button class="action-btn del" onclick="deleteMessage('${msg.id}')" title="Delete">🗑️</button>
      ` : ''}
    </div>`;

  area.appendChild(row);
  if (scroll) scrollToBottom(true);
}

function updateMessageInDOM(msg) {
  const row = document.getElementById(`msg-${msg.id}`);
  if (!row) return;
  row.dataset.content = msg.content;
  const bubble = row.querySelector('.bubble');
  if (bubble) bubble.textContent = msg.content;
  const meta = row.querySelector('.bubble-meta');
  if (meta && msg.isEdited) {
    if (!meta.querySelector('.edited-tag')) {
      const tag = document.createElement('span');
      tag.className = 'edited-tag';
      tag.textContent = 'edited';
      meta.prepend(tag);
    }
  }
}

function markDeletedInDOM(msgId) {
  const row = document.getElementById(`msg-${msgId}`);
  if (!row) return;
  const bubble = row.querySelector('.bubble');
  if (bubble) {
    bubble.innerHTML = `<span class="deleted-bubble">🚫 This message was deleted</span>`;
  }
  const actions = row.querySelector('.msg-actions');
  if (actions) actions.innerHTML = '';
}

function appendSystem(text) {
  const area = document.getElementById('messages-area');
  const div  = document.createElement('div');
  div.className = 'system-msg';
  div.textContent = text;
  area.appendChild(div);
  scrollToBottom(true);
  lastAuthorId = null;
}

function scrollToBottom(smooth = true) {
  const area = document.getElementById('messages-area');
  area.scrollTo({ top: area.scrollHeight, behavior: smooth ? 'smooth' : 'instant' });
}

// ── Context menu ──────────────────────────────────────────
function openCtxMenu(e, msgId, bubbleEl) {
  e.preventDefault();
  const row = document.getElementById(`msg-${msgId}`);
  ctxMsgId      = msgId;
  ctxMsgContent = row?.dataset.content || '';
  ctxIsMe       = row?.dataset.isMe === '1';

  const menu = document.getElementById('ctx-menu');
  document.getElementById('ctx-edit').style.display   = ctxIsMe ? '' : 'none';
  document.getElementById('ctx-delete').style.display = ctxIsMe ? '' : 'none';

  menu.style.left = Math.min(e.clientX, window.innerWidth  - 180) + 'px';
  menu.style.top  = Math.min(e.clientY, window.innerHeight - 120) + 'px';
  menu.classList.add('open');
}

function closeCtxMenu() {
  document.getElementById('ctx-menu').classList.remove('open');
}

function ctxEdit()   { if (ctxMsgId) startEdit(ctxMsgId, ctxMsgContent); }
function ctxCopy()   { copyText(ctxMsgContent); closeCtxMenu(); }
function ctxDelete() { if (ctxMsgId) deleteMessage(ctxMsgId); }

function copyText(text) {
  navigator.clipboard.writeText(text || '').then(() => toast('Copied!', 'success'));
}

document.addEventListener('click', e => {
  if (!document.getElementById('ctx-menu').contains(e.target)) closeCtxMenu();
});

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
    err.textContent = data.message || 'Failed.';
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
    return `<div class="online-user">
      <div class="avatar sm">${escHtml(init)}<span class="status-badge ${status}"></span></div>
      <span class="name">${escHtml(u.username)}</span>
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

  hub.on('ReceiveMessage', msg => {
    if (msg.roomId === currentRoomId) {
      appendMessage(msg, true);
    } else {
      unreadCounts[msg.roomId] = (unreadCounts[msg.roomId] || 0) + 1;
      loadRooms();
    }
  });

  hub.on('ReceivePrivateMessage', msg => {
    toast(`💬 ${msg.senderUsername}: ${msg.content.slice(0, 60)}`, 'info');
  });

  hub.on('UserJoinedRoom', ({ userId, roomId }) => {
    if (roomId === currentRoomId && userId !== myUserId) appendSystem('Someone joined');
  });

  hub.on('UserLeftRoom', ({ userId, roomId }) => {
    if (roomId === currentRoomId && userId !== myUserId) appendSystem('Someone left');
  });

  hub.on('UserOnline',  () => loadOnlineUsers());
  hub.on('UserOffline', () => loadOnlineUsers());

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
    toast('Connected ✓', 'success');
    if (currentRoomId) hub.invoke('JoinRoom', currentRoomId).catch(() => {});
    loadOnlineUsers();
  });
  hub.onclose(() => toast('Disconnected', 'error'));

  try {
    await hub.start();
  } catch {
    toast('Real-time unavailable — using REST', 'info');
  }
}

// ── Logout ────────────────────────────────────────────────
function logout() {
  if (hub) hub.stop().catch(() => {});
  localStorage.clear();
  location.href = '/';
}

// ── Toast ─────────────────────────────────────────────────
function toast(msg, type = 'info') {
  const c  = document.getElementById('toast-container');
  const el = document.createElement('div');
  const icon = type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ';
  el.className = `toast ${type}`;
  el.innerHTML = `<span style="font-size:.9rem">${icon}</span><span>${escHtml(msg)}</span>`;
  c.appendChild(el);
  requestAnimationFrame(() => el.classList.add('show'));
  setTimeout(() => { el.classList.remove('show'); setTimeout(() => el.remove(), 350); }, 3500);
}

// ── Helpers ───────────────────────────────────────────────
function escHtml(s) {
  return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// Modal keyboard
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') { closeNewRoomModal(); closeCtxMenu(); }
});
document.getElementById('new-room-modal').addEventListener('click', e => {
  if (e.target === e.currentTarget) closeNewRoomModal();
});

// ── Start ─────────────────────────────────────────────────
init();
