// Redirect to chat if already logged in
if (localStorage.getItem('token')) {
  window.location.href = '/chat.html';
}

function switchTab(tab) {
  const isLogin = tab === 'login';
  document.getElementById('login-form').style.display    = isLogin ? '' : 'none';
  document.getElementById('register-form').style.display = isLogin ? 'none' : '';
  document.getElementById('tab-login').classList.toggle('active', isLogin);
  document.getElementById('tab-register').classList.toggle('active', !isLogin);
  clearErrors();
}

function clearErrors() {
  ['login-error', 'register-error'].forEach(id => {
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

async function handleLogin(e) {
  e.preventDefault();
  clearErrors();
  const btn = document.getElementById('login-btn');
  btn.disabled = true;
  btn.textContent = 'Signing in…';

  try {
    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email:    document.getElementById('login-email').value,
        password: document.getElementById('login-password').value
      })
    });

    const data = await res.json();

    if (!res.ok) {
      showError('login-error', data.message || 'Login failed.');
      return;
    }

    localStorage.setItem('token',    data.token);
    localStorage.setItem('userId',   data.userId);
    localStorage.setItem('username', data.username);
    window.location.href = '/chat.html';

  } catch {
    showError('login-error', 'Network error. Is the server running?');
  } finally {
    btn.disabled = false;
    btn.textContent = 'Sign In';
  }
}

async function handleRegister(e) {
  e.preventDefault();
  clearErrors();
  const btn = document.getElementById('register-btn');
  btn.disabled = true;
  btn.textContent = 'Creating account…';

  try {
    const res = await fetch('/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: document.getElementById('reg-username').value,
        email:    document.getElementById('reg-email').value,
        password: document.getElementById('reg-password').value
      })
    });

    const data = await res.json();

    if (!res.ok) {
      const msg = data.errors ? data.errors.join(' ') : (data.message || 'Registration failed.');
      showError('register-error', msg);
      return;
    }

    localStorage.setItem('token',    data.token);
    localStorage.setItem('userId',   data.userId);
    localStorage.setItem('username', data.username);
    window.location.href = '/chat.html';

  } catch {
    showError('register-error', 'Network error. Is the server running?');
  } finally {
    btn.disabled = false;
    btn.textContent = 'Create Account';
  }
}
