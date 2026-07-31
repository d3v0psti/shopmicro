// --- CONFIGURAÇÃO DO BACKEND ---
const API_URL = '';

// --- ESTADO DA APLICAÇÃO ---
let cart = JSON.parse(localStorage.getItem('shopmicro_cart')) || [];
let currentUser = JSON.parse(localStorage.getItem('shopmicro_user')) || null;

let allProducts = [];
let discountPercent = 0;
let shippingCost = 0;

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
    } else {
      allProducts = [
        { id: '1', name: 'Notebook Gamer Pro', price: 4500.00, stock: 5, imageUrl: 'https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=300' },
        { id: '2', name: 'Mouse Sem Fio RGB', price: 150.00, stock: 12, imageUrl: 'https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=300' },
        { id: '3', name: 'Teclado Mecânico Switch Blue', price: 320.00, stock: 8, imageUrl: 'https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=300' },
        { id: '4', name: 'Monitor Ultrawide 29"', price: 1250.00, stock: 3, imageUrl: 'https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=300' },
        { id: '5', name: 'Headset Surround 7.1', price: 280.00, stock: 15, imageUrl: 'https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=300' }
      ];
    }

    renderProducts(allProducts);
  } catch (error) {
    console.error('Erro ao carregar produtos:', error);
    if (productsList) productsList.innerHTML = `<p class="empty-msg">Erro ao carregar produtos.</p>`;
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
    const pId = product.id || product.Id;
    const pName = product.name || product.Name;
    const pPrice = Number(product.price || product.Price || 0);
    const pStock = product.stockQuantity || product.StockQuantity || product.stock || product.Stock || 10;
    const pImg = product.imageUrl || product.ImageUrl || defaultImages[index % defaultImages.length];

    return `
      <div class="product-card">
        <img src="${pImg}" alt="${pName}" class="product-img">
        <h3>${pName}</h3>
        <p class="price">R$ ${pPrice.toFixed(2)}</p>
        <small style="color: #64748b; margin-bottom: 0.5rem; display: block;">Estoque: ${pStock} un.</small>
        <button class="btn-primary" onclick="addToCart('${pId}', '${escapeString(pName)}', ${pPrice}, ${pStock})">
          🛒 Adicionar
        </button>
      </div>
    `;
  }).join('');
}

function escapeString(str) { return str.replace(/'/g, "\\'"); }

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

  const totalItems = cart.reduce((sum, i) => sum + i.quantity, 0);
  const subtotal = cart.reduce((sum, i) => sum + (i.price * i.quantity), 0);
  const discountVal = subtotal * discountPercent;
  const grandTotal = Math.max(0, subtotal - discountVal + shippingCost);

  if (cartCount) cartCount.textContent = totalItems;
  if (cartTotal) cartTotal.textContent = `R$ ${grandTotal.toFixed(2)}`;

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

      const orderPayload = {
        customerName: document.getElementById('customer-name').value,
        customerEmail: document.getElementById('customer-email').value,
        customerCpf: document.getElementById('customer-cpf').value,
        customerPhone: document.getElementById('customer-phone').value,
        customerCep: document.getElementById('customer-cep').value,
        customerAddress: document.getElementById('customer-address').value,
        paymentMethod: document.getElementById('payment-method').value,
        items: cart.map(i => ({
          productId: i.productId,
          quantity: i.quantity
        }))
      };

      try {
        const response = await fetch(`${API_URL}/api/v1/orders`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(orderPayload)
        });

        if (!response.ok) {
          const errData = await response.json().catch(() => ({}));
          throw new Error(errData.message || 'Erro ao finalizar pedido.');
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

        currentUser = newUserData;
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
        await fetch(`${API_URL}/api/users/${currentUser.email}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(updatedUser)
        });
      } catch (err) {
        console.warn('Atualização offline realizada.', err);
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
          const activeOrders = userOrders.filter(order => order.status !== 'Cancelled');

          if (activeOrders.length > 0) {
            if (deleteModal) deleteModal.classList.add('hidden');
            showToast('⚠️ Você possui pedidos ativos. Cancele-os em "Meus Pedidos" antes de excluir a conta.', 'error');
            return;
          }
        }

        const response = await fetch(`${API_URL}/api/users/${currentUser.email}`, {
          method: 'DELETE'
        });

        if (!response.ok) {
          throw new Error('Erro ao deletar conta no servidor.');
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

  ordersList.innerHTML = orders.map(order => {
    const status = order.status || 'Pending';
    const statusLabel = orderStatusLabels[status] || status;
    const itemsSummary = (order.items || [])
      .map(i => `${i.quantity}x ${i.productName}`)
      .join(', ');

    const canCancel = status === 'Pending' || status === 'Confirmed';

    return `
      <div class="order-card">
        <div class="order-card-header">
          <span class="order-id">Pedido #${order.id.slice(0, 8)}</span>
          <span class="order-status ${status.toLowerCase()}">${statusLabel}</span>
        </div>
        <div class="order-items-mini">${itemsSummary}</div>
        <div class="order-total">Total: R$ ${Number(order.totalAmount).toFixed(2)}</div>
        ${canCancel ? `<button class="btn-cancel-order" onclick="cancelOrder('${order.id}')">Cancelar Pedido</button>` : ''}
      </div>
    `;
  }).join('');
}

async function cancelOrder(orderId) {
  if (!confirm('Tem certeza que deseja cancelar este pedido? O estoque será devolvido.')) return;

  try {
    const res = await fetch(`${API_URL}/api/v1/orders/${orderId}/cancel`, { method: 'POST' });

    if (!res.ok) {
      const errData = await res.json().catch(() => ({}));
      throw new Error(errData.message || 'Não foi possível cancelar o pedido.');
    }

    showToast('Pedido cancelado com sucesso.', 'success');
    loadMyOrders();
  } catch (err) {
    console.error('Erro ao cancelar pedido:', err);
    showToast(err.message || 'Erro ao cancelar o pedido.', 'error');
  }
}

function updateUserUI() {
  const userDisplay = document.getElementById('user-display');
  const btnOpenLogin = document.getElementById('btn-open-login');
  const btnUserProfile = document.getElementById('btn-user-profile');

  if (currentUser) {
    if (userDisplay) userDisplay.textContent = currentUser.fullName || currentUser.email;
    if (btnUserProfile) btnUserProfile.classList.remove('hidden');
    if (btnOpenLogin) btnOpenLogin.textContent = '🚪 Sair';
  } else {
    if (userDisplay) userDisplay.textContent = '';
    if (btnUserProfile) btnUserProfile.classList.add('hidden');
    if (btnOpenLogin) btnOpenLogin.textContent = '🔑 Entrar';
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
  toast.textContent = message;

  container.appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}
