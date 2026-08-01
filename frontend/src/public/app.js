// --- CONFIGURAÇÃO DO BACKEND ---
const API_URL = '';

// --- ESTADO DA APLICAÇÃO ---
let cart = JSON.parse(localStorage.getItem('shopmicro_cart')) || [];
let currentUser = JSON.parse(localStorage.getItem('shopmicro_user')) || null;

let allProducts = [];
let backendAvailable = false;
let discountPercent = 0;
let shippingCost = 0;

async function fetchWithAuth(url, options = {}) {
  if (!currentUser?.token) {
    throw new Error('Sua sessão expirou. Entre novamente para continuar.');
  }

  const requestOptions = {
    ...options,
    headers: {
      ...(options.headers || {}),
      Authorization: `Bearer ${currentUser.token}`
    }
  };

  let response = await fetch(url, requestOptions);
  if (response.status !== 401) return response;

  const refreshResponse = await fetch(`${API_URL}/api/users/refresh-token`, {
    method: 'POST'
  });

  if (!refreshResponse.ok) {
    throw new Error('Sua sessão expirou. Entre novamente para continuar.');
  }

  const { token } = await refreshResponse.json();
  currentUser = { ...currentUser, token };
  localStorage.setItem('shopmicro_user', JSON.stringify(currentUser));
  requestOptions.headers.Authorization = `Bearer ${token}`;

  return fetch(url, requestOptions);
}

const defaultImages = [
  'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=300',
  'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300',
  'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=300'
];

// --- INICIALIZAÇÃO ---
document.addEventListener('DOMContentLoaded', () => {
  loadProducts();
  updateUserUI();
  updateCart();
  setupEventListeners();
  setupAuthEvents();
  setupProfileModalEvents();
  setupCheckoutStepper();
  setupAutoCepEvents();
  setupCartDrawerEvents();
  setupOrdersModalEvents();
});

// --- DRAWER DO CARRINHO (abre/fecha por clique, como em e-commerce real) ---
function setupCartDrawerEvents() {
  const btnOpenCart = document.getElementById('btn-open-cart');
  const btnCloseCart = document.getElementById('btn-close-cart');
  const cartDrawer = document.getElementById('cart-drawer');
  const cartOverlay = document.getElementById('cart-overlay');

  const openCart = () => {
    if (cartDrawer) cartDrawer.classList.add('open');
    if (cartOverlay) cartOverlay.classList.remove('hidden');
  };

  const closeCart = () => {
    if (cartDrawer) cartDrawer.classList.remove('open');
    if (cartOverlay) cartOverlay.classList.add('hidden');
  };

  if (btnOpenCart) btnOpenCart.addEventListener('click', openCart);
  if (btnCloseCart) btnCloseCart.addEventListener('click', closeCart);
  if (cartOverlay) cartOverlay.addEventListener('click', closeCart);

  // Exposto globalmente para outras funções (ex: abrir carrinho após login)
  window.openCartDrawer = openCart;
  window.closeCartDrawer = closeCart;
}

// --- AUTO-BUSCA CEP VIA API (ViaCEP) ---
function setupAutoCepEvents() {
  const fetchAddressByCep = async (cepInputId, addressInputId) => {
    const cepInput = document.getElementById(cepInputId);
    const addressInput = document.getElementById(addressInputId);
    
    if (!cepInput || !addressInput) return;

    cepInput.addEventListener('blur', async () => {
      const cep = cepInput.value.replace(/\D/g, '');
      if (cep.length === 8) {
        try {
          const res = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
          const data = await res.json();
          if (!data.erro) {
            addressInput.value = `${data.logradouro}, , ${data.bairro} - ${data.localidade}/${data.uf}`;
            showToast('Endereço localizado pelo CEP!', 'success');
          } else {
            showToast('CEP não localizado.', 'warning');
          }
        } catch {
          console.log('Erro ao consultar CEP');
        }
      }
    });
  };

  fetchAddressByCep('reg-cep', 'reg-address');
  fetchAddressByCep('edit-cep', 'edit-address');
  fetchAddressByCep('customer-cep', 'customer-address');
}

// --- CARREGAR PRODUTOS (Com rota correta /api/v1/products) ---
async function loadProducts() {
  const productsList = document.getElementById('products-list');

  try {
    const response = await fetch(`${API_URL}/api/v1/products`).catch(() => null);
    
    if (response && response.ok) {
      allProducts = await response.json();
      backendAvailable = true;
    } else {
      backendAvailable = false;
      allProducts = [
        { id: '1', name: 'Notebook Gamer Pro', price: 4500.00, stock: 5, imageUrl: 'https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=300' },
        { id: '2', name: 'Mouse Sem Fio RGB', price: 150.00, stock: 12, imageUrl: 'https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=300' },
        { id: '3', name: 'Teclado Mecânico Switch Blue', price: 320.00, stock: 8, imageUrl: 'https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=300' },
        { id: '4', name: 'Monitor Ultrawide 29"', price: 1250.00, stock: 3, imageUrl: 'https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=300' },
        { id: '5', name: 'Headset Surround 7.1', price: 280.00, stock: 15, imageUrl: 'https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=300' }
      ];
    }

    renderProducts(allProducts);
    sanitizeCartItems();
  } catch (error) {
    console.error('Erro ao carregar produtos:', error);
    if (productsList) productsList.innerHTML = `<p class="empty-msg">Erro ao carregar produtos.</p>`;
  }
}

function sanitizeCartItems() {
  const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const validProductIds = new Set(allProducts.map(p => p.id || p.Id).filter(Boolean));
  const originalLength = cart.length;

  cart = cart.filter(item => {
    const id = item.productId;
    if (!id || typeof id !== 'string' || !guidRegex.test(id)) return false;
    if (validProductIds.size > 0 && !validProductIds.has(id)) return false;
    return true;
  });

  if (cart.length !== originalLength) {
    saveCartToStorage();
    updateCart();
  }
}

function renderProducts(products) {
  const productsList = document.getElementById('products-list');
  const productCount = document.getElementById('product-count');

  if (productCount) productCount.textContent = `${products.length} produtos`;
  if (!productsList) return;

  if (products.length === 0) {
    productsList.innerHTML = `<p class="empty-msg">Nenhum produto encontrado.</p>`;
    return;
  }

  productsList.innerHTML = products.map((product, index) => {
    const pName = product.name || product.Name || 'Produto';
    const pPrice = Number(product.price || product.Price || 0);
    const pStock = product.stockQuantity || product.StockQuantity || product.stock || product.Stock || 0;
    const pImg = product.imageUrl || product.ImageUrl || defaultImages[index % defaultImages.length];
    const pCategory = product.category || product.Category || 'Categoria geral';
    const pDescription = (product.description || product.Description || 'Sem descrição disponível.').trim();
    const shortDescription = pDescription.length > 120 ? `${pDescription.slice(0, 117)}...` : pDescription;

    return `
      <article class="product-card">
        <div class="product-card-media">
          <img src="${pImg}" alt="${pName}" class="product-img">
          <span class="product-badge">${pCategory}</span>
        </div>
        <div class="product-card-body">
          <div class="product-card-header">
            <h3>${pName}</h3>
            <span class="product-price">R$ ${pPrice.toFixed(2)}</span>
          </div>
          <p class="product-description">${shortDescription}</p>
          <div class="product-card-meta">
            <span class="product-stock">${pStock} unidades em estoque</span>
            <button class="btn-primary btn-add-cart" data-product-index="${index}">Adicionar ao carrinho</button>
          </div>
        </div>
      </article>
    `;
  }).join('');

  productsList.querySelectorAll('.btn-add-cart').forEach(button => {
    button.addEventListener('click', () => {
      const product = products[Number(button.dataset.productIndex)];
      if (!product) return;

      const id = product.id || product.Id;
      const name = product.name || product.Name || 'Produto';
      const price = Number(product.price || product.Price || 0);
      const stock = product.stockQuantity || product.StockQuantity || product.stock || product.Stock || 0;
      addToCart(id, name, price, stock);
    });
  });
}

// --- LÓGICA DO CARRINHO ---
function saveCartToStorage() { localStorage.setItem('shopmicro_cart', JSON.stringify(cart)); }

function addToCart(id, name, price, stock) {
  const product = allProducts.find(p => (p.id || p.Id) === id);
  const img = product?.imageUrl || product?.ImageUrl || defaultImages[0];

  const item = cart.find(i => i.productId === id);
  if (item) {
    if (item.quantity + 1 > stock) {
      showToast('Limite de estoque atingido!', 'warning');
      return;
    }
    item.quantity += 1;
  } else {
    cart.push({ productId: id, name, price, quantity: 1, stock, img });
  }
  
  saveCartToStorage();
  showToast(`"${name}" adicionado ao carrinho!`, 'success');
  updateCart();
}

function changeQuantity(id, delta) {
  const item = cart.find(i => i.productId === id);
  if (!item) return;

  const newQty = item.quantity + delta;
  if (newQty > item.stock) {
    showToast('Estoque máximo atingido!', 'warning');
    return;
  }
  if (newQty <= 0) {
    removeFromCart(id);
    return;
  }

  item.quantity = newQty;
  saveCartToStorage();
  updateCart();
}

function removeFromCart(id) {
  cart = cart.filter(i => i.productId !== id);
  saveCartToStorage();
  showToast('Item removido.', 'info');
  updateCart();
}

function clearCart() {
  if (cart.length === 0) return;
  if (confirm('Deseja realmente limpar todo o carrinho?')) {
    cart = [];
    saveCartToStorage();
    showToast('Carrinho esvaziado.', 'info');
    updateCart();
  }
}

function updateCart() {
  const cartList = document.getElementById('cart-items');
  const cartCount = document.getElementById('cart-count');
  const cartTotal = document.getElementById('cart-total');
  const cartDrawerSummary = document.getElementById('cart-drawer-summary');

  const totalItems = cart.reduce((sum, i) => sum + i.quantity, 0);
  const subtotal = cart.reduce((sum, i) => sum + (i.price * i.quantity), 0);
  const discountVal = subtotal * discountPercent;
  const grandTotal = Math.max(0, subtotal - discountVal + shippingCost);

  if (cartCount) cartCount.textContent = totalItems;
  if (cartTotal) cartTotal.textContent = `R$ ${grandTotal.toFixed(2)}`;
  if (cartDrawerSummary) cartDrawerSummary.textContent = totalItems ? `${totalItems} ${totalItems === 1 ? 'item selecionado' : 'itens selecionados'} para você.` : 'Sua seleção aparecerá aqui.';

  if (document.getElementById('subtotal-val')) document.getElementById('subtotal-val').textContent = `R$ ${subtotal.toFixed(2)}`;
  if (document.getElementById('discount-val')) document.getElementById('discount-val').textContent = `- R$ ${discountVal.toFixed(2)}`;
  if (document.getElementById('shipping-val')) document.getElementById('shipping-val').textContent = `R$ ${shippingCost.toFixed(2)}`;
  if (document.getElementById('total-val')) document.getElementById('total-val').textContent = `R$ ${grandTotal.toFixed(2)}`;
  if (document.getElementById('step2-total-val')) document.getElementById('step2-total-val').textContent = `R$ ${grandTotal.toFixed(2)}`;

  renderMiniSummary();

  if (!cartList) return;

  if (cart.length === 0) {
    cartList.innerHTML = `
      <div class="empty-cart-state">
        <span>🛒</span>
        <p>Seu carrinho está vazio</p>
      </div>
    `;
    return;
  }

  cartList.innerHTML = cart.map(i => `
    <div class="cart-item-card">
      <img src="${i.img}" alt="${i.name}" class="cart-item-img">
      <div class="cart-item-info">
        <h4>${i.name}</h4>
        <span class="cart-item-price">R$ ${(i.price * i.quantity).toFixed(2)}</span>
        
        <div class="qty-controls">
          <button class="qty-btn" onclick="changeQuantity('${i.productId}', -1)">-</button>
          <span class="qty-val">${i.quantity}</span>
          <button class="qty-btn" onclick="changeQuantity('${i.productId}', 1)">+</button>
        </div>
      </div>
      <button class="btn-remove" onclick="removeFromCart('${i.productId}')">🗑️</button>
    </div>
  `).join('');
}

function renderMiniSummary() {
  const summaryBox = document.getElementById('checkout-items-summary');
  if (!summaryBox) return;

  summaryBox.innerHTML = cart.map(item => `
    <div class="mini-item-row">
      <span>${item.quantity}x ${item.name}</span>
      <strong>R$ ${(item.price * item.quantity).toFixed(2)}</strong>
    </div>
  `).join('');
}

// --- PREENCHIMENTO AUTOMÁTICO DO CHECKOUT ---
function fillCheckoutWithUserData() {
  const autofillAlert = document.getElementById('autofill-alert');

  if (currentUser) {
    if (document.getElementById('customer-name')) document.getElementById('customer-name').value = currentUser.fullName || '';
    if (document.getElementById('customer-cpf')) document.getElementById('customer-cpf').value = currentUser.cpf || '';
    if (document.getElementById('customer-email')) document.getElementById('customer-email').value = currentUser.email || '';
    if (document.getElementById('customer-phone')) document.getElementById('customer-phone').value = currentUser.phone || '';
    if (document.getElementById('customer-cep')) document.getElementById('customer-cep').value = currentUser.cep || '';
    if (document.getElementById('customer-address')) document.getElementById('customer-address').value = currentUser.address || '';

    if (autofillAlert) autofillAlert.classList.remove('hidden');
  } else {
    if (autofillAlert) autofillAlert.classList.add('hidden');
  }
}

// --- FLUXO DO CHECKOUT (ETAPAS) (Com rota correta /api/v1/orders) ---
function setupCheckoutStepper() {
  const btnGoStep2 = document.getElementById('btn-go-step2');
  const btnBackStep1 = document.getElementById('btn-back-step1');
  const step1Content = document.getElementById('cart-step-1');
  const step2Content = document.getElementById('cart-step-2');
  const stepBtn1 = document.getElementById('step-btn-1');
  const stepBtn2 = document.getElementById('step-btn-2');
  const checkoutForm = document.getElementById('checkout-form');

  if (btnGoStep2) {
    btnGoStep2.addEventListener('click', () => {
      if (cart.length === 0) {
        showToast('Seu carrinho está vazio!', 'warning');
        return;
      }

      if (!currentUser) {
        openAuthModal('login');
        showToast('Faça login para continuar a compra.', 'warning');
        return;
      }

      fillCheckoutWithUserData();
      step1Content.classList.add('hidden');
      step2Content.classList.remove('hidden');
      stepBtn1.classList.remove('active');
      stepBtn2.classList.add('active');
    });
  }

  if (btnBackStep1) {
    btnBackStep1.addEventListener('click', () => {
      step2Content.classList.add('hidden');
      step1Content.classList.remove('hidden');
      stepBtn2.classList.remove('active');
      stepBtn1.classList.add('active');
    });
  }

  if (checkoutForm) {
    checkoutForm.addEventListener('submit', async (e) => {
      e.preventDefault();

      if (!checkoutForm.checkValidity()) {
        checkoutForm.reportValidity();
        return;
      }

      const submitBtn = document.getElementById('btn-confirm-order') || document.querySelector('#checkout-form button[type="submit"]');

      const orderPayload = {
        customerName: document.getElementById('customer-name').value,
        customerEmail: document.getElementById('customer-email').value,
        customerCpf: document.getElementById('customer-cpf').value,
        customerPhone: document.getElementById('customer-phone').value,
        customerCep: document.getElementById('customer-cep').value,
        customerAddress: document.getElementById('customer-address').value,
        paymentMethod: document.getElementById('payment-method').value,
        items: cart.map(i => ({ productId: i.productId, quantity: i.quantity }))
      };

      // Validações cliente-side rápidas para feedback mais claro
      if (!orderPayload.items || orderPayload.items.length === 0) {
        showToast('Seu carrinho está vazio. Adicione itens antes de finalizar.', 'warning');
        return;
      }

      if (!backendAvailable) {
        showToast('Backend indisponível. Aguarde e tente novamente.', 'error');
        console.error('Checkout abortado: backend indisponível');
        return;
      }

      // Se o frontend estiver rodando por proxy no Docker Compose, API_URL
      // pode ser vazio e as requisições devem ser relativas.
      const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
      const invalidItems = orderPayload.items.filter(it => !it.productId || typeof it.productId !== 'string' || !guidRegex.test(it.productId));
      if (invalidItems.length > 0) {
        const names = invalidItems.map(it => it.productId || '(sem id)').slice(0, 5).join(', ');
        const confirmMsg = `Seu carrinho contém ${invalidItems.length} item(s) com identificador inválido (ex: ${names}).\nDeseja removê-los automaticamente e continuar?`;
        if (confirm(confirmMsg)) {
          // Remove itens inválidos do carrinho e persiste
          cart = cart.filter(i => i.productId && typeof i.productId === 'string' && guidRegex.test(i.productId));
          saveCartToStorage();
          updateCart();
          showToast('Itens inválidos removidos do carrinho. Tente finalizar novamente.', 'info');
        } else {
          showToast('Checkout cancelado. Remova itens inválidos para continuar.', 'warning');
        }
        return;
      }

      if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.dataset.origText = submitBtn.textContent;
        submitBtn.textContent = 'Enviando pedido...';
      }

      try {
        const base = API_URL || '';
        const url = `${base}/api/v1/orders`;
        console.log('Checkout enviando payload', orderPayload, 'para', url);
        const response = await fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
          body: JSON.stringify(orderPayload)
        });

        if (!response.ok) {
          let errMsg = 'Erro ao finalizar pedido.';
          let responseText = null;
          try {
            const cloned = response.clone();
            const errData = await response.json().catch(() => null);
            responseText = await cloned.text().catch(() => null);

            // Tratamento para ValidationProblemDetails do ASP.NET
            if (errData) {
              if (errData.message) {
                errMsg = errData.message;
              } else if (errData.errors && typeof errData.errors === 'object') {
                const parts = [];
                for (const k of Object.keys(errData.errors)) {
                  const arr = errData.errors[k];
                  if (Array.isArray(arr)) parts.push(...arr);
                  else parts.push(String(arr));
                }
                if (parts.length) errMsg = parts.join(' | ');
              } else if (errData.title) {
                errMsg = errData.title;
              }
            } else if (responseText) {
              errMsg = responseText;
            }
          } catch (readErr) {
            console.error('Erro lendo corpo de erro do servidor no checkout:', readErr);
          }

          console.error('Checkout falhou', {
            status: response.status,
            statusText: response.statusText,
            payload: orderPayload,
            serverMessage: errMsg,
            responseText
          });

          throw new Error(errMsg + ` (status ${response.status})`);
        }

        showToast(`🎉 Pedido confirmado! Obrigado, ${orderPayload.customerName}.`, 'success');

        cart = [];
        saveCartToStorage();

        step2Content.classList.add('hidden');
        step1Content.classList.remove('hidden');
        stepBtn2.classList.remove('active');
        stepBtn1.classList.add('active');

        updateCart();
        if (window.closeCartDrawer) window.closeCartDrawer();
      } catch (err) {
        console.error('Erro no checkout:', err);
        showToast(err.message || 'Erro ao conectar com o servidor.', 'error');
      } finally {
        if (submitBtn) {
          submitBtn.disabled = false;
          if (submitBtn.dataset.origText) submitBtn.textContent = submitBtn.dataset.origText;
        }
      }
    });
  }
}

// --- MODAL DE AUTENTICAÇÃO ---
function openAuthModal(defaultTab = 'login') {
  const modal = document.getElementById('auth-modal');
  const loginForm = document.getElementById('login-form');
  const registerForm = document.getElementById('register-form');

  modal.classList.remove('hidden');

  if (defaultTab === 'login') {
    loginForm.classList.remove('hidden');
    registerForm.classList.add('hidden');
  } else {
    registerForm.classList.remove('hidden');
    loginForm.classList.add('hidden');
  }
}

function setupAuthEvents() {
  const modal = document.getElementById('auth-modal');
  const btnOpenLogin = document.getElementById('btn-open-login');
  const btnCloseModal = document.getElementById('btn-close-modal');
  const linkToRegister = document.getElementById('link-to-register');
  const linkToLogin = document.getElementById('link-to-login');
  const loginForm = document.getElementById('login-form');
  const registerForm = document.getElementById('register-form');

  if (btnOpenLogin) {
    btnOpenLogin.addEventListener('click', () => {
      if (currentUser) {
        currentUser = null;
        localStorage.removeItem('shopmicro_user');
        updateUserUI();
        updateCart();
        showToast('Você saiu da conta.', 'info');
      } else {
        openAuthModal('login');
      }
    });
  }

  if (btnCloseModal) btnCloseModal.addEventListener('click', () => modal.classList.add('hidden'));

  if (linkToRegister) linkToRegister.addEventListener('click', (e) => { e.preventDefault(); openAuthModal('register'); });
  if (linkToLogin) linkToLogin.addEventListener('click', (e) => { e.preventDefault(); openAuthModal('login'); });

  // SUBMIT LOGIN
  if (loginForm) {
    loginForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const email = document.getElementById('login-email').value;
      const password = document.getElementById('login-password').value;

      try {
        const res = await fetch(`${API_URL}/api/users/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password })
        });

        if (!res.ok) {
          throw new Error('E-mail ou senha incorretos, ou usuário não cadastrado.');
        }

        const userData = await res.json();

        currentUser = userData;
        localStorage.setItem('shopmicro_user', JSON.stringify(currentUser));

        modal.classList.add('hidden');
        updateUserUI();
        showToast(`Bem-vindo(a), ${currentUser.fullName || currentUser.email}!`, 'success');

        if (cart.length > 0) {
          document.getElementById('btn-go-step2').click();
        }

      } catch (err) {
        console.error('Erro no login:', err);
        showToast(err.message || 'Falha ao realizar login. Verifique seus dados.', 'error');
      }
    });
  }

  // SUBMIT CADASTRO
  if (registerForm) {
    registerForm.addEventListener('submit', async (e) => {
      e.preventDefault();

      const passwordValue = document.getElementById('reg-password').value;

      const newUserData = {
        fullName: document.getElementById('reg-name').value,
        email: document.getElementById('reg-email').value,
        password: passwordValue,
        passwordHash: passwordValue, 
        cpf: document.getElementById('reg-cpf').value,
        phone: document.getElementById('reg-phone').value,
        cep: document.getElementById('reg-cep').value,
        address: document.getElementById('reg-address').value
      };

      try {
        const response = await fetch(`${API_URL}/api/users/register`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(newUserData)
        });

        if (!response.ok) {
          const errorData = await response.json().catch(() => ({}));
          throw new Error(errorData.message || 'Erro ao registrar usuário no servidor.');
        }

        const loginResponse = await fetch(`${API_URL}/api/users/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email: newUserData.email, password: passwordValue })
        });

        if (!loginResponse.ok) {
          throw new Error('Conta criada, mas não foi possível iniciar a sessão. Faça login para continuar.');
        }

        currentUser = await loginResponse.json();
        localStorage.setItem('shopmicro_user', JSON.stringify(currentUser));

        modal.classList.add('hidden');
        updateUserUI();
        showToast('Conta criada com sucesso no Banco de Dados!', 'success');

        if (cart.length > 0) {
          document.getElementById('btn-go-step2').click();
        }

      } catch (err) {
        console.error('Erro no cadastro:', err);
        showToast(err.message || 'Erro ao conectar com o servidor.', 'error');
      }
    });
  }
}

// --- EDITAR PERFIL E DELETAR CONTA (Com modal customizado e validação de pedidos) ---
function setupProfileModalEvents() {
  const profileModal = document.getElementById('profile-modal');
  const btnCloseProfileModal = document.getElementById('btn-close-profile-modal');
  const btnUserProfile = document.getElementById('btn-user-profile');
  const btnEditFromCheckout = document.getElementById('btn-edit-from-checkout');
  const editProfileForm = document.getElementById('edit-profile-form');

  // Elementos do modal de confirmação de exclusão
  const deleteModal = document.getElementById('delete-confirm-modal');
  const btnCancelDelete = document.getElementById('btn-cancel-delete');
  const btnConfirmDelete = document.getElementById('btn-confirm-delete');

  const openProfileModal = () => {
    if (!currentUser) return;

    document.getElementById('edit-name').value = currentUser.fullName || '';
    document.getElementById('edit-email').value = currentUser.email || '';
    document.getElementById('edit-phone').value = currentUser.phone || '';
    document.getElementById('edit-cpf').value = currentUser.cpf || '';
    document.getElementById('edit-cep').value = currentUser.cep || '';
    document.getElementById('edit-address').value = currentUser.address || '';

    profileModal.classList.remove('hidden');
  };

  if (btnUserProfile) btnUserProfile.addEventListener('click', openProfileModal);
  if (btnEditFromCheckout) btnEditFromCheckout.addEventListener('click', openProfileModal);
  if (btnCloseProfileModal) btnCloseProfileModal.addEventListener('click', () => profileModal.classList.add('hidden'));

  if (editProfileForm) {
    editProfileForm.addEventListener('submit', async (e) => {
      e.preventDefault();

      const updatedUser = {
        fullName: document.getElementById('edit-name').value,
        email: document.getElementById('edit-email').value,
        phone: document.getElementById('edit-phone').value,
        cpf: document.getElementById('edit-cpf').value,
        cep: document.getElementById('edit-cep').value,
        address: document.getElementById('edit-address').value
      };

      try {
        const response = await fetchWithAuth(`${API_URL}/api/users/${encodeURIComponent(currentUser.email)}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(updatedUser)
        });
        if (!response.ok) throw new Error('Não foi possível atualizar o perfil.');
      } catch (err) {
        console.warn('Falha ao atualizar o perfil.', err);
        showToast(err.message || 'Não foi possível atualizar o perfil.', 'error');
        return;
      }

      currentUser = { ...currentUser, ...updatedUser };
      localStorage.setItem('shopmicro_user', JSON.stringify(currentUser));

      profileModal.classList.add('hidden');
      updateUserUI();
      fillCheckoutWithUserData();
      showToast('Perfil atualizado com sucesso!', 'success');
    });
  }

  const btnDeleteAccount = document.getElementById('btn-delete-account');
  if (btnDeleteAccount) {
    btnDeleteAccount.addEventListener('click', () => {
      if (!currentUser || !currentUser.email) return;
      if (deleteModal) deleteModal.classList.remove('hidden');
    });
  }

  if (btnCancelDelete) {
    btnCancelDelete.addEventListener('click', () => {
      if (deleteModal) deleteModal.classList.add('hidden');
    });
  }

  if (btnConfirmDelete) {
    btnConfirmDelete.addEventListener('click', async () => {
      if (!currentUser || !currentUser.email) return;

      try {
        // Validação: verifica se há pedidos ATIVOS (não cancelados) antes de
        // deletar a conta. Pedidos já cancelados não bloqueiam a exclusão.
        const ordersRes = await fetch(`${API_URL}/api/v1/orders?email=${encodeURIComponent(currentUser.email)}`).catch(() => null);

        if (ordersRes && ordersRes.ok) {
          const userOrders = await ordersRes.json();
          const activeOrders = userOrders.filter(order => order.status !== 'Cancelled' && order.status !== 3);

          if (activeOrders.length > 0) {
            if (deleteModal) deleteModal.classList.add('hidden');
            showToast('⚠️ Você possui pedidos ativos. Cancele-os em "Meus Pedidos" antes de excluir a conta.', 'error');
            return;
          }
        }

        const response = await fetchWithAuth(`${API_URL}/api/users/${encodeURIComponent(currentUser.email)}`, {
          method: 'DELETE'
        });

        if (!response.ok) {
          const errorData = await response.json().catch(() => null);
          throw new Error(errorData?.message || 'Erro ao deletar conta no servidor.');
        }

        localStorage.removeItem('shopmicro_user');
        currentUser = null;

        if (deleteModal) deleteModal.classList.add('hidden');
        profileModal.classList.add('hidden');

        updateUserUI();
        updateCart();

        showToast('Sua conta foi deletada com sucesso.', 'info');

      } catch (err) {
        console.error('Erro ao deletar:', err);
        if (deleteModal) deleteModal.classList.add('hidden');
        showToast(err.message || 'Erro ao conectar com o servidor.', 'error');
      }
    });
  }
}

// --- MEUS PEDIDOS (listar e cancelar) ---
const orderStatusLabels = {
  Pending: 'Pendente',
  Confirmed: 'Confirmado',
  Shipped: 'Enviado',
  Cancelled: 'Cancelado'
};

function setupOrdersModalEvents() {
  const ordersModal = document.getElementById('orders-modal');
  const btnViewOrders = document.getElementById('btn-view-orders');
  const btnCloseOrdersModal = document.getElementById('btn-close-orders-modal');

  if (btnViewOrders) {
    btnViewOrders.addEventListener('click', () => {
      if (!currentUser) return;
      ordersModal.classList.remove('hidden');
      loadMyOrders();
    });
  }

  if (btnCloseOrdersModal) {
    btnCloseOrdersModal.addEventListener('click', () => ordersModal.classList.add('hidden'));
  }
}

async function loadMyOrders() {
  const ordersList = document.getElementById('orders-list');
  if (!ordersList || !currentUser) return;

  ordersList.innerHTML = `<p class="loading">Carregando pedidos...</p>`;

  try {
    const res = await fetch(`${API_URL}/api/v1/orders?email=${encodeURIComponent(currentUser.email)}`);
    if (!res.ok) throw new Error('Não foi possível carregar seus pedidos.');

    const orders = await res.json();
    renderMyOrders(orders);
  } catch (err) {
    console.error('Erro ao carregar pedidos:', err);
    ordersList.innerHTML = `<p class="empty-msg">Erro ao carregar seus pedidos.</p>`;
  }
}

function renderMyOrders(orders) {
  const ordersList = document.getElementById('orders-list');
  if (!ordersList) return;

  if (!orders || orders.length === 0) {
    ordersList.innerHTML = `<p class="empty-msg">Você ainda não tem pedidos.</p>`;
    return;
  }
  // Helper para normalizar status que pode vir como string ou number
  const normalizeStatusKey = (raw) => {
    // Se for número (enum serializado como inteiro), mapeia pelo índice
    if (typeof raw === 'number') {
      const map = ['Pending', 'Confirmed', 'Shipped', 'Cancelled'];
      return map[raw] ?? 'Pending';
    }
    if (typeof raw === 'string') {
      // Pode vir em minúsculas ou já como string do enum
      const up = raw.charAt(0).toUpperCase() + raw.slice(1);
      if (['Pending', 'Confirmed', 'Shipped', 'Cancelled'].includes(up)) return up;
      // tenta extrair nome se vier acompanhado (ex: "OrderStatus.Pending")
      const parts = raw.split('.');
      const candidate = parts[parts.length - 1];
      const cUp = candidate.charAt(0).toUpperCase() + candidate.slice(1);
      return ['Pending', 'Confirmed', 'Shipped', 'Cancelled'].includes(cUp) ? cUp : 'Pending';
    }
    return 'Pending';
  };

  ordersList.innerHTML = orders.map(order => {
    const statusKey = normalizeStatusKey(order.status);
    const statusLabel = orderStatusLabels[statusKey] || statusKey;
    const itemsSummary = (order.items || [])
      .map(i => `${i.quantity || i.Quantity || 0}x ${i.productName || i.ProductName || 'Produto'}`)
      .join(', ');

    const canCancel = statusKey === 'Pending' || statusKey === 'Confirmed';

    return `
      <div class="order-card">
        <div class="order-card-header">
          <span class="order-id">Pedido #${String(order.id).slice(0, 8)}</span>
          <span class="order-status ${statusKey.toLowerCase()}">${statusLabel}</span>
        </div>
        <div class="order-items-mini">${itemsSummary}</div>
        <div class="order-total">Total: R$ ${Number(order.totalAmount).toFixed(2)}</div>
        ${canCancel ? `<button class="btn-cancel-order" data-order-id="${order.id}" onclick="cancelOrder('${order.id}', this)">Cancelar Pedido</button>` : ''}
      </div>
    `;
  }).join('');
}

async function cancelOrder(orderId) {
  if (!confirm('Tem certeza que deseja cancelar este pedido? O estoque será devolvido.')) return;

  // Tenta localizar o botão (se passado como segundo argumento via onclick, ele
  // será o elemento `btn` — caso contrário, procuramos pelo atributo data-order-id).
  let buttonEl = null;
  try { buttonEl = arguments[1] ?? document.querySelector(`button.btn-cancel-order[data-order-id="${orderId}"]`); } catch {}

  const originalText = buttonEl ? buttonEl.textContent : null;
  if (buttonEl) {
    buttonEl.disabled = true;
    buttonEl.textContent = 'Cancelando...';
  }

  try {
    const base = API_URL || '';
    const url = `${base}/api/v1/orders/${orderId}/cancel`;
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Accept': 'application/json' }
    });

    if (!res.ok) {
      // Tenta ler um JSON com a mensagem do servidor, senão lê texto cru para diagnóstico
      let errMsg = 'Não foi possível cancelar o pedido.';
      try {
        const errData = await res.json().catch(() => null);
        if (errData && errData.message) errMsg = errData.message;
        else {
          const txt = await res.text().catch(() => null);
          if (txt) errMsg = txt;
        }
      } catch (readErr) {
        console.error('Erro lendo corpo de erro do servidor:', readErr);
      }
      throw new Error(errMsg + ` (status ${res.status})`);
    }

    showToast('Pedido cancelado com sucesso.', 'success');
    // Pequena espera para evitar condições de corrida entre commit da transação
    // e a leitura imediata dos pedidos em alguns ambientes/discos lentos.
    setTimeout(() => loadMyOrders(), 250);
  } catch (err) {
    console.error('Erro ao cancelar pedido:', err);
    showToast(err.message || 'Erro ao cancelar o pedido.', 'error');
    if (buttonEl) {
      buttonEl.disabled = false;
      if (originalText) buttonEl.textContent = originalText;
    }
  }
}

function updateUserUI() {
  const userDisplay = document.getElementById('user-display');
  const btnOpenLogin = document.getElementById('btn-open-login');
  const btnUserProfile = document.getElementById('btn-user-profile');

  if (currentUser) {
    if (userDisplay) userDisplay.textContent = currentUser.fullName || currentUser.email;
    if (btnUserProfile) btnUserProfile.classList.remove('hidden');
    if (btnOpenLogin) btnOpenLogin.textContent = 'Sair';
  } else {
    if (userDisplay) userDisplay.textContent = '';
    if (btnUserProfile) btnUserProfile.classList.add('hidden');
    if (btnOpenLogin) btnOpenLogin.textContent = 'Entrar';
  }
}

// --- DEMAIS EVENTOS ---
function setupEventListeners() {
  const searchInput = document.getElementById('search-input');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      const term = e.target.value.toLowerCase();
      const filtered = allProducts.filter(p => (p.name || p.Name).toLowerCase().includes(term));
      renderProducts(filtered);
    });
  }

  const btnApplyCoupon = document.getElementById('btn-apply-coupon');
  if (btnApplyCoupon) {
    btnApplyCoupon.addEventListener('click', () => {
      const code = document.getElementById('coupon-code').value.trim().toUpperCase();
      if (code === 'SHOP10') {
        discountPercent = 0.10;
        showToast('Cupom de 10% aplicado!', 'success');
      } else {
        discountPercent = 0;
        showToast('Cupom inválido!', 'error');
      }
      updateCart();
    });
  }

  const btnCalcShipping = document.getElementById('btn-calc-shipping');
  if (btnCalcShipping) {
    btnCalcShipping.addEventListener('click', () => {
      const cep = document.getElementById('shipping-cep').value.trim();
      if (cep.length === 8 && !isNaN(cep)) {
        shippingCost = 15.00;
        document.getElementById('shipping-info').textContent = 'Frete Normal: R$ 15,00 (3-5 dias)';
        showToast('Frete calculado com sucesso!', 'info');
      } else {
        shippingCost = 0;
        document.getElementById('shipping-info').textContent = 'CEP Inválido!';
        showToast('Digite um CEP válido com 8 dígitos.', 'warning');
      }
      updateCart();
    });
  }
}

function showToast(message, type = 'info') {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  const icon = { success: '✓', error: '!', warning: '!', info: 'i' }[type] || 'i';
  toast.innerHTML = `<span class="toast-icon">${icon}</span><span class="toast-message"></span>`;
  toast.querySelector('.toast-message').textContent = message;

  if (type === 'success' && message.includes('adicionado ao carrinho')) {
    toast.classList.add('cart-toast');
    const action = document.createElement('button');
    action.type = 'button';
    action.className = 'toast-action';
    action.textContent = 'Ver carrinho';
    action.addEventListener('click', () => {
      toast.remove();
      window.openCartDrawer?.();
    });
    toast.appendChild(action);
  }

  container.appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}
