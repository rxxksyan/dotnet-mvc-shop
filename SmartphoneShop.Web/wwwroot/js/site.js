function getCsrfToken() {
    var meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.getAttribute('content') : '';
}

function addToComparison(smartphoneId, button) {
    const isIn = button && button.classList.contains('in-comparison');
    const url = isIn ? '/comparison/toggle' : '/comparison/add';

    fetch(url, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest', 'X-CSRF-TOKEN': getCsrfToken() },
        body: 'smartphoneId=' + smartphoneId
    })
    .then(r => r.text())
    .then(text => {
        if (text) {
            try {
                var d = JSON.parse(text);
                if (d.success === false) {
                    showNotification(d.message || 'В сравнении может быть не более 4 товаров');
                    return;
                }
            } catch(e) { console.error('Parse error:', e, 'text:', text); }
        }
        if (button) {
            const inComp = !isIn;
            button.style.background = inComp ? '#D44177' : '#3a3a3a';
            button.classList.toggle('in-comparison', inComp);
        }
        updateComparisonBadge();
    })
    .catch(error => {
        if (error instanceof TypeError && error.message.includes('login')) {
            window.location.href = '/account/login';
        }
        console.error('Error:', error);
    });
}

function showNotification(message) {
    var existing = document.querySelector('.comparison-notification');
    if (existing) existing.remove();

    var note = document.createElement('div');
    note.className = 'comparison-notification';
    note.textContent = message;
    note.style.cssText = 'position:fixed;bottom:24px;left:50%;transform:translateX(-50%);background:#D44177;color:#fff;padding:12px 24px;border-radius:8px;font-size:14px;z-index:9999;animation:fadeInOut 2.5s ease forwards;';
    document.body.appendChild(note);
    setTimeout(function () { note.remove(); }, 2500);
}

function updateComparisonBadge() {
    fetch('/comparison/count', { credentials: 'same-origin' })
        .then(r => r.ok ? r.json() : Promise.reject())
        .then(data => {
            const badge = document.querySelector('.comparison-badge');
            if (badge) {
                badge.textContent = data.count;
                badge.style.display = data.count > 0 ? 'inline-flex' : 'none';
            }
        })
        .catch(() => {});
}

function toggleFavorite(btn, smartphoneId) {
    var form = btn.closest('form');
    var formData = new FormData(form);
    if (!formData.has('__RequestVerificationToken')) {
        formData.append('__RequestVerificationToken', getCsrfToken());
    }
    fetch(form.action, {
        method: 'POST',
        credentials: 'same-origin',
        body: new URLSearchParams(formData)
    })
    .then(() => {
        btn.classList.toggle('active');
        var svg = btn.querySelector('svg path');
        if (svg) svg.setAttribute('fill', btn.classList.contains('active') ? '#D44177' : 'none');
    })
    .catch(e => console.error(e));
}

function addToCart(id) {
    fetch('/cart/add', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-CSRF-TOKEN': getCsrfToken() },
        body: 'smartphoneId=' + id
    })
    .then(response => {
        if (response.ok) {
            updateCartBadge();
        } else if (response.status === 401) {
            window.location.href = '/account/login';
        }
    })
    .catch(e => console.error(e));
}

function updateCartBadge() {
    fetch('/cart/count', { credentials: 'same-origin' })
        .then(r => r.ok ? r.json() : Promise.reject())
        .then(data => {
            const badge = document.querySelector('.cart-badge');
            if (badge) {
                badge.textContent = data.count;
                badge.style.display = data.count > 0 ? 'inline-flex' : 'none';
            }
        })
        .catch(() => {});
}

function toggleFavoriteDetail(btn, id) {
    fetch('/favorites/toggle', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-CSRF-TOKEN': getCsrfToken() },
        body: 'smartphoneId=' + id
    })
    .then(() => {
        btn.classList.toggle('active');
        var svg = btn.querySelector('svg path');
        if (svg) svg.setAttribute('fill', btn.classList.contains('active') ? '#D44177' : 'none');
    })
    .catch(e => console.error(e));
}

function addToComparisonFromDetail(id) {
    fetch('/comparison/add', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-CSRF-TOKEN': getCsrfToken() },
        body: 'smartphoneId=' + id
    })
    .then(response => {
        if (response.ok) {
            alert('Товар добавлен к сравнению');
            updateComparisonBadge();
        } else if (response.status === 401) {
            window.location.href = '/account/login';
        } else if (response.status === 400) {
            alert('В сравнении может быть не более 4 товаров');
        }
    })
    .catch(e => console.error(e));
}

document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        document.querySelector(this.getAttribute('href')).scrollIntoView({
            behavior: 'smooth'
        });
    });
});

updateComparisonBadge();
updateCartBadge();
