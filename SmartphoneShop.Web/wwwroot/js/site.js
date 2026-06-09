function addToComparison(smartphoneId, button) {
    const isIn = button && button.style.background.includes('#D44177');
    const url = isIn ? '/comparison/remove' : '/comparison/add';

    fetch(url, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'smartphoneId=' + smartphoneId
    })
    .then(response => {
        if (response.ok) {
            if (button) {
                const inComp = !isIn;
                button.style.background = inComp ? '#D44177' : '#3a3a3a';
                button.classList.toggle('in-comparison', inComp);
            }
            updateComparisonBadge();
        } else if (response.status === 401) {
            window.location.href = '/account/login';
        } else if (response.status === 400) {
            alert('В сравнении может быть не более 4 товаров');
        }
    })
    .catch(error => console.error('Error:', error));
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
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
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
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
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
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
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
