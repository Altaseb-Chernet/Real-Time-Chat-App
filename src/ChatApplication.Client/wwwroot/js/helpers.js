// ── OrbitChat JS Helpers ──────────────────────────────────────────────────

function scrollToBottom(id) {
    const el = document.getElementById(id);
    if (el) el.scrollTop = el.scrollHeight;
}

// Theme management
function getTheme() {
    const stored = localStorage.getItem('orbit-theme');
    if (stored) return stored;
    return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
}

function setTheme(theme, save = true) {
    document.documentElement.setAttribute('data-theme', theme);
    if (save) localStorage.setItem('orbit-theme', theme);
    const meta = document.querySelector('meta[name="theme-color"]');
    if (meta) meta.content = theme === 'light' ? '#FFFFFF' : '#0B0F1A';
}

function toggleTheme() {
    const current = document.documentElement.getAttribute('data-theme') || 'dark';
    const next = current === 'dark' ? 'light' : 'dark';
    setTheme(next, true);
    return next;
}

// OS theme sync
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    if (!localStorage.getItem('orbit-theme')) {
        setTheme(e.matches ? 'dark' : 'light', false);
    }
});

// Apply saved theme on load
(function() {
    const theme = getTheme();
    setTheme(theme, !!localStorage.getItem('orbit-theme'));
})();

// Expose to Blazor
window.scrollToBottom = scrollToBottom;
window.orbitTheme = { getTheme, setTheme, toggleTheme };

// Resizable Sidebars
function initResizer() {
    const layout = document.querySelector('.chat-layout');
    if (!layout) return;

    let leftResizer = document.querySelector('.resizer-left');
    if (!leftResizer) {
        leftResizer = document.createElement('div');
        leftResizer.className = 'resizer resizer-left';
        layout.insertBefore(leftResizer, layout.children[1]); 
    }

    let rightResizer = document.querySelector('.resizer-right');
    if (!rightResizer) {
        rightResizer = document.createElement('div');
        rightResizer.className = 'resizer resizer-right';
        layout.insertBefore(rightResizer, layout.children[3]); 
    }

    let isResizing = false;
    let currentResizer = null;

    function onMouseMove(e) {
        if (!isResizing) return;
        if (currentResizer === 'left') {
            let newWidth = e.clientX;
            if (newWidth < 150) newWidth = 0; // Collapse threshold
            else if (newWidth < 250) newWidth = 250; // Minimum usable width
            else if (newWidth > 600) newWidth = 600; // Maximum width
            
            if (newWidth === 0) {
                document.querySelector('.sidebar')?.classList.remove('is-open'); // For mobile compat
            } else {
                document.documentElement.style.setProperty('--sidebar-width', newWidth + 'px');
            }
        } else if (currentResizer === 'right') {
            let newWidth = window.innerWidth - e.clientX;
            if (newWidth < 150) newWidth = 0;
            else if (newWidth < 220) newWidth = 220;
            else if (newWidth > 600) newWidth = 600;
            
            document.documentElement.style.setProperty('--right-panel-width', newWidth + 'px');
        }
    }

    function onMouseUp() {
        if (isResizing) {
            isResizing = false;
            currentResizer = null;
            document.body.style.cursor = 'default';
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            document.body.style.userSelect = 'auto'; 
        }
    }

    leftResizer.addEventListener('mousedown', (e) => {
        isResizing = true;
        currentResizer = 'left';
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none'; 
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });

    rightResizer.addEventListener('mousedown', (e) => {
        isResizing = true;
        currentResizer = 'right';
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
}

window.orbitResizer = { init: initResizer };
