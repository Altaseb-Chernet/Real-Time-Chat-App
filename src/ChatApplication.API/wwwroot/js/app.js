// Redirect if already logged in
if (localStorage.getItem('token')) location.href = '/chat.html';

function switchTab(tab) {
  const isLogin = tab === 'login';
  document.getElementById('login-form').style.display    = isLogin ? '' : 'none';
  document.getElementById('register-form').style.display = isLogin ? 'none' : '';
  document.getElementById('tab-login').classList.toggle('active', isLogin);
  document.getElementById('tab-register').classList.toggle('active', !isLogin);
  clearErrors();
}

function clearErrors() {
  ['login-error','register-error'].forEach(id => {
    const el = document.getElementById(id);
    el.classList.remove('show');
    el.textContent = '';
  });
}

function showError(id, msg) {
  const el = document.getElementById(id);
  el.textContent = msg;
  el.classList.add('show');
}

function setLoading(prefix, loading) {
  document.getElementById(`${prefix}-btn`).disabled = loading;
  document.getElementById(`${prefix}-btn-text`).style.display  = loading ? 'none' : '';
  document.getElementById(`${prefix}-spinner`).style.display   = loading ? '' : 'none';
}

async function handleLogin(e) {
  e.preventDefault();
  clearErrors();
  setLoading('login', true);
  try {
    const res  = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email:    document.getElementById('login-email').value.trim(),
        password: document.getElementById('login-password').value
      })
    });
    const data = await res.json();
    if (!res.ok) { showError('login-error', data.message || 'Login failed.'); return; }
    saveSession(data);
    location.href = '/chat.html';
  } catch {
    showError('login-error', 'Cannot reach server. Is the app running?');
  } finally {
    setLoading('login', false);
  }
}

async function handleRegister(e) {
  e.preventDefault();
  clearErrors();
  setLoading('register', true);
  try {
    const res  = await fetch('/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: document.getElementById('reg-username').value.trim(),
        email:    document.getElementById('reg-email').value.trim(),
        password: document.getElementById('reg-password').value
      })
    });
    const data = await res.json();
    if (!res.ok) {
      const msg = data.errors ? data.errors.join(' ') : (data.message || 'Registration failed.');
      showError('register-error', msg);
      return;
    }
    saveSession(data);
    location.href = '/chat.html';
  } catch {
    showError('register-error', 'Cannot reach server. Is the app running?');
  } finally {
    setLoading('register', false);
  }
}

function saveSession(data) {
  localStorage.setItem('token',    data.token);
  localStorage.setItem('userId',   data.userId);
  localStorage.setItem('username', data.username);
}
