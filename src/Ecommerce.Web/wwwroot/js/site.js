// Global E-Commerce Client Scripts
document.addEventListener('DOMContentLoaded', function () {
    // Initial badge sync
    updateCartCountBadge();
    updateWishlistCountBadge();
});

// Toast notification helper using SweetAlert2
function showToast(icon, title) {
    if (typeof Swal !== 'undefined') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 2500,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
        Toast.fire({
            icon: icon,
            title: title
        });
    } else {
        alert(title);
    }
}

// Add to Cart via AJAX
function addToCart(productId, variantId = null, quantity = 1) {
    fetch('/Cart/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            productId: parseInt(productId),
            productVariantId: variantId ? parseInt(variantId) : null,
            quantity: parseInt(quantity)
        })
    })
    .then(res => {
        if (res.redirected) {
            window.location.href = res.url;
            return null;
        }
        return res.json();
    })
    .then(data => {
        if (!data) return;
        if (data.redirect) {
            window.location.href = data.redirect;
            return;
        }
        if (data.success) {
            showToast('success', data.message || 'Added to cart!');
            if (data.cartCount !== undefined) {
                setCartBadge(data.cartCount);
            }
        } else {
            showToast('error', data.message || 'Failed to add item.');
        }
    })
    .catch(err => {
        console.error(err);
        showToast('error', 'Failed to add item to cart.');
    });
}

// Buy / Order Now -> Direct 1-Click Single Product Checkout (Leaves cart items untouched)
function buyNow(productId, variantId = null, quantity = 1, btnElement = null) {
    if (btnElement) {
        btnElement.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span> Opening Direct Checkout...';
        btnElement.disabled = true;
    }

    const vParam = (variantId && parseInt(variantId) > 0) ? `&variantId=${encodeURIComponent(variantId)}` : '';
    const qParam = quantity ? parseInt(quantity) : 1;
    
    // Direct redirect to Checkout with specific product parameters
    window.location.href = `/Checkout?productId=${encodeURIComponent(productId)}${vParam}&quantity=${encodeURIComponent(qParam)}`;
}

// Toggle Wishlist via AJAX
function toggleWishlist(productId, btnElement) {
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `productId=${productId}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.redirect) {
            window.location.href = data.redirect;
            return;
        }

        if (data.success) {
            showToast('success', data.message);
            if (btnElement) {
                const icon = btnElement.querySelector('i');
                if (data.added) {
                    btnElement.classList.add('active');
                    if (icon) {
                        icon.classList.remove('bi-heart');
                        icon.classList.add('bi-heart-fill');
                    }
                } else {
                    btnElement.classList.remove('active');
                    if (icon) {
                        icon.classList.remove('bi-heart-fill');
                        icon.classList.add('bi-heart');
                    }
                }
            }
            if (data.count !== undefined) {
                setWishlistBadge(data.count);
            }
        } else {
            showToast('error', data.message || 'Action failed.');
        }
    })
    .catch(err => {
        console.error(err);
        showToast('error', 'Wishlist update failed.');
    });
}

// Update Cart Quantity
function updateCartQty(cartItemId, newQty) {
    if (newQty < 1) {
        removeFromCart(cartItemId);
        return;
    }

    fetch('/Cart/UpdateQuantity', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `cartItemId=${cartItemId}&quantity=${newQty}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            const itemTotalEl = document.getElementById(`item-total-${cartItemId}`);
            if (itemTotalEl) itemTotalEl.innerText = data.itemTotal;

            const subTotalEl = document.getElementById('cart-subtotal');
            if (subTotalEl) subTotalEl.innerText = data.subTotal;

            const discountEl = document.getElementById('cart-discount');
            if (discountEl) discountEl.innerText = data.discount;

            const taxEl = document.getElementById('cart-tax');
            if (taxEl) taxEl.innerText = data.tax;

            const shippingEl = document.getElementById('cart-shipping');
            if (shippingEl) shippingEl.innerText = data.shipping;

            const grandTotalEl = document.getElementById('cart-grandtotal');
            if (grandTotalEl) grandTotalEl.innerText = data.grandTotal;

            setCartBadge(data.cartCount);
        }
    })
    .catch(err => console.error(err));
}

// Remove from Cart
function removeFromCart(cartItemId) {
    fetch('/Cart/Remove', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `cartItemId=${cartItemId}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            const row = document.getElementById(`cart-row-${cartItemId}`);
            if (row) {
                row.remove();
            }
            setCartBadge(data.cartCount);
            showToast('info', data.message);
            // Reload page if no items left
            if (data.cartCount === 0) {
                window.location.reload();
            }
        }
    })
    .catch(err => console.error(err));
}

// Apply Coupon
function applyCoupon() {
    const input = document.getElementById('couponCodeInput');
    if (!input || !input.value.trim()) {
        showToast('warning', 'Please enter a coupon code.');
        return;
    }

    fetch('/Cart/ApplyCoupon', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `code=${encodeURIComponent(input.value.trim())}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast('success', data.message);
            setTimeout(() => window.location.reload(), 1000);
        } else {
            showToast('error', data.message);
        }
    })
    .catch(err => console.error(err));
}

// Remove Coupon
function removeCoupon() {
    fetch('/Cart/RemoveCoupon', {
        method: 'POST'
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast('info', data.message);
            setTimeout(() => window.location.reload(), 600);
        }
    })
    .catch(err => console.error(err));
}

// Badge Helpers
function setCartBadge(count) {
    const badge = document.getElementById('navCartCount');
    if (badge) {
        badge.innerText = count;
        badge.style.display = count > 0 ? 'flex' : 'none';
    }
}

function setWishlistBadge(count) {
    const badge = document.getElementById('navWishlistCount');
    if (badge) {
        badge.innerText = count;
        badge.style.display = count > 0 ? 'flex' : 'none';
    }
}

function updateCartCountBadge() {
    fetch('/Cart/GetCartCount')
        .then(res => res.json())
        .then(data => {
            if (data.count !== undefined) setCartBadge(data.count);
        })
        .catch(() => {});
}

function updateWishlistCountBadge() {
    fetch('/Wishlist/GetCount')
        .then(res => res.json())
        .then(data => {
            if (data.count !== undefined) setWishlistBadge(data.count);
        })
        .catch(() => {});
}

// Thumbnail switcher for Product Details
function switchProductImage(thumbElement, newSrc) {
    const mainImg = document.getElementById('mainProductImage');
    if (mainImg) {
        mainImg.src = newSrc;
    }
    document.querySelectorAll('.thumbnail-box').forEach(el => el.classList.remove('active-thumb'));
    thumbElement.classList.add('active-thumb');
}

// Global Password Visibility Toggle
document.addEventListener('click', function (e) {
    const toggleBtn = e.target.closest('.password-toggle-btn');
    if (!toggleBtn) return;
    
    e.preventDefault();

    let input = null;
    const targetSelector = toggleBtn.getAttribute('data-target');
    if (targetSelector) {
        input = document.querySelector(targetSelector);
    } else {
        const inputGroup = toggleBtn.closest('.input-group');
        if (inputGroup) {
            input = inputGroup.querySelector('input');
        }
    }

    if (!input) return;

    const icon = toggleBtn.querySelector('i');
    const textSpan = toggleBtn.querySelector('.toggle-text');

    if (input.type === 'password') {
        input.type = 'text';
        if (icon) {
            icon.classList.remove('bi-eye');
            icon.classList.add('bi-eye-slash', 'text-primary');
        }
        if (textSpan) {
            textSpan.innerText = 'Hide';
        }
    } else {
        input.type = 'password';
        if (icon) {
            icon.classList.remove('bi-eye-slash', 'text-primary');
            icon.classList.add('bi-eye');
        }
        if (textSpan) {
            textSpan.innerText = 'Show';
        }
    }
});

// Global Click on Product Card -> Navigate to Product Details
document.addEventListener('click', function (e) {
    const card = e.target.closest('.product-card');
    if (!card) return;

    // Ignore clicks on interactive controls (wishlist toggle, add to cart button, inputs)
    if (e.target.closest('button, .wishlist-btn-corner, .btn-add-cart-mini, input, select, textarea, label')) {
        return;
    }

    // Check data-href or anchor link inside the card
    const dataHref = card.getAttribute('data-href');
    if (dataHref) {
        window.location.href = dataHref;
        return;
    }

    const detailLink = card.querySelector('a.product-title') || card.querySelector('a.product-img-wrap') || card.querySelector('a[href*="/Shop/Details"]');
    if (detailLink && detailLink.href) {
        window.location.href = detailLink.href;
    }
});

// ================= INSTANT LIVE SEARCH AUTOCOMPLETE =================
function setupLiveSearch(inputId, resultsId) {
    const input = document.getElementById(inputId);
    const resultsContainer = document.getElementById(resultsId);
    if (!input || !resultsContainer) return;

    let debounceTimer = null;
    let activeIndex = -1;
    let currentResults = [];

    // Highlight query text inside name
    function highlightQuery(text, query) {
        if (!query) return text;
        const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const regex = new RegExp(`(${escaped})`, 'gi');
        return text.replace(regex, '<span class="text-primary fw-bold">$1</span>');
    }

    // Render HTML inside dropdown
    function renderResults(data, query) {
        currentResults = data.results || [];
        activeIndex = -1;

        if (!currentResults || currentResults.length === 0) {
            resultsContainer.innerHTML = `
                <div class="quick-search-empty">
                    <i class="bi bi-search fs-3 text-muted mb-2 d-block"></i>
                    <p class="mb-1 fw-semibold text-dark">No products found for "${query}"</p>
                    <small class="text-muted">Try searching with different keywords or browse all categories.</small>
                </div>
            `;
            resultsContainer.classList.remove('d-none');
            return;
        }

        const itemsHtml = currentResults.map((item, index) => {
            const imgUrl = item.imageUrl || 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=100';
            const priceHtml = item.originalPrice && item.discountPercentage > 0
                ? `<span class="quick-search-orig-price">${item.originalPrice}</span><span class="quick-search-price text-danger">${item.price}</span>`
                : `<span class="quick-search-price text-dark">${item.price}</span>`;

            const badgeHtml = item.discountPercentage > 0
                ? `<span class="badge bg-danger-subtle text-danger border border-danger-subtle ms-1">${item.discountPercentage}% OFF</span>`
                : '';

            const stockHtml = item.inStock
                ? `<span class="text-success small"><i class="bi bi-check-circle-fill me-1"></i>In Stock</span>`
                : `<span class="text-danger small"><i class="bi bi-x-circle-fill me-1"></i>Out of Stock</span>`;

            return `
                <a href="${item.url}" class="quick-search-item" data-index="${index}">
                    <img src="${imgUrl}" alt="${item.name}" class="quick-search-thumb" onerror="this.src='https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=100';" />
                    <div class="quick-search-info">
                        <div class="quick-search-title">${highlightQuery(item.name, query)}</div>
                        <div class="quick-search-meta">
                            ${item.categoryName ? `<span class="quick-search-cat">${item.categoryName}</span>` : ''}
                            ${item.brandName ? `<span>• ${item.brandName}</span>` : ''}
                            <span>• ${stockHtml}</span>
                        </div>
                    </div>
                    <div class="quick-search-price-wrap">
                        <div>${priceHtml}</div>
                        ${badgeHtml ? `<div class="mt-1">${badgeHtml}</div>` : ''}
                    </div>
                </a>
            `;
        }).join('');

        const totalCount = data.total || currentResults.length;
        const viewAllUrl = data.viewAllUrl || `/Shop?SearchTerm=${encodeURIComponent(query)}`;

        resultsContainer.innerHTML = `
            <div class="quick-search-header">
                <span><i class="bi bi-stars text-primary me-1"></i> Quick Search Results</span>
                <span class="badge bg-light text-secondary border">${totalCount} item${totalCount === 1 ? '' : 's'}</span>
            </div>
            <div class="quick-search-list">
                ${itemsHtml}
            </div>
            <div class="quick-search-footer">
                <a href="${viewAllUrl}" class="quick-search-view-all">
                    <span>View all matching products (${totalCount})</span>
                    <i class="bi bi-arrow-right"></i>
                </a>
            </div>
        `;

        resultsContainer.classList.remove('d-none');
    }

    // Trigger search fetch
    function executeSearch() {
        const query = input.value.trim();
        if (query.length < 2) {
            resultsContainer.classList.add('d-none');
            resultsContainer.innerHTML = '';
            currentResults = [];
            return;
        }

        resultsContainer.innerHTML = `
            <div class="quick-search-spinner">
                <div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div>
                <span>Searching "${query}"...</span>
            </div>
        `;
        resultsContainer.classList.remove('d-none');

        fetch(`/Shop/QuickSearch?q=${encodeURIComponent(query)}`)
            .then(res => res.json())
            .then(data => {
                renderResults(data, query);
            })
            .catch(err => {
                console.error('QuickSearch error:', err);
                resultsContainer.classList.add('d-none');
            });
    }

    // Input debounce listener
    input.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(executeSearch, 220);
    });

    // Input focus listener
    input.addEventListener('focus', function () {
        if (input.value.trim().length >= 2 && resultsContainer.innerHTML.trim() !== '') {
            resultsContainer.classList.remove('d-none');
        }
    });

    // Keyboard navigation (ArrowDown, ArrowUp, Enter, Escape)
    input.addEventListener('keydown', function (e) {
        if (resultsContainer.classList.contains('d-none') || currentResults.length === 0) {
            return;
        }

        const items = resultsContainer.querySelectorAll('.quick-search-item');
        if (!items || items.length === 0) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            activeIndex++;
            if (activeIndex >= items.length) activeIndex = 0;
            updateActiveItem(items);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            activeIndex--;
            if (activeIndex < 0) activeIndex = items.length - 1;
            updateActiveItem(items);
        } else if (e.key === 'Enter') {
            if (activeIndex >= 0 && activeIndex < items.length) {
                e.preventDefault();
                items[activeIndex].click();
            }
        } else if (e.key === 'Escape') {
            resultsContainer.classList.add('d-none');
        }
    });

    function updateActiveItem(items) {
        items.forEach((it, idx) => {
            if (idx === activeIndex) {
                it.classList.add('active');
                it.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            } else {
                it.classList.remove('active');
            }
        });
    }

    // Click outside handler
    document.addEventListener('click', function (e) {
        if (!input.contains(e.target) && !resultsContainer.contains(e.target)) {
            resultsContainer.classList.add('d-none');
        }
    });
}

// Initialize live search for Desktop and Mobile search bars on DOM load
document.addEventListener('DOMContentLoaded', function () {
    setupLiveSearch('desktopSearchInput', 'desktopQuickSearchResults');
    setupLiveSearch('mobileSearchInput', 'mobileQuickSearchResults');
});

