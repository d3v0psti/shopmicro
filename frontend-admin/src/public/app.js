const BACKEND_URL = window.location.origin + '/api/v1';

const productForm = document.getElementById('product-form');
const productName = document.getElementById('product-name');
const productDescription = document.getElementById('product-description');
const productPrice = document.getElementById('product-price');
const productStock = document.getElementById('product-stock');
const productCategory = document.getElementById('product-category');
const categorySuggestions = document.getElementById('category-suggestions');
const productImage = document.getElementById('product-image');
const productImageUrl = document.getElementById('product-image-url');
const previewCard = document.getElementById('preview-card');
const previewImage = document.getElementById('preview-image');
const previewName = document.getElementById('preview-name');
const previewPrice = document.getElementById('preview-price');
const previewStock = document.getElementById('preview-stock');
const previewCategory = document.getElementById('preview-category');
const productList = document.getElementById('product-list');
const userList = document.getElementById('user-list');
const userRegisterForm = document.getElementById('user-register-form');
const userEmail = document.getElementById('user-email');
const userFullName = document.getElementById('user-fullname');
const userPassword = document.getElementById('user-password');
const userCpf = document.getElementById('user-cpf');
const userPhone = document.getElementById('user-phone');
const userCep = document.getElementById('user-cep');
const userAddress = document.getElementById('user-address');
const changePasswordForm = document.getElementById('change-password-form');
const currentPassword = document.getElementById('current-password');
const newPassword = document.getElementById('new-password');
const toastContainer = document.getElementById('toast-container');
const loginSection = document.getElementById('admin-login-section');
const adminAppSection = document.getElementById('admin-app-section');
const loginForm = document.getElementById('login-form');
const loginEmail = document.getElementById('login-email');
const loginPassword = document.getElementById('login-password');
const logoutButton = document.getElementById('logout-button');
const adminUserActions = document.getElementById('admin-user-actions');
const adminUserEmail = document.getElementById('admin-user-email');

let selectedProductId = null;
let selectedImageFile = null;
let allProducts = [];
let filteredProducts = [];
let selectedCategoryFilter = '';
let searchQuery = '';
let currentPage = 1;
const PAGE_SIZE = 8;
const AUTH_TOKEN_KEY = 'shopmicro_admin_token';
const AUTH_EMAIL_KEY = 'shopmicro_admin_email';

const getAuthToken = () => localStorage.getItem(AUTH_TOKEN_KEY);
const setAuthToken = token => localStorage.setItem(AUTH_TOKEN_KEY, token);
const setAuthEmail = email => localStorage.setItem(AUTH_EMAIL_KEY, email);
const clearAuthStorage = () => {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem(AUTH_EMAIL_KEY);
};

const authHeaders = () => {
  const token = getAuthToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
};

const showToast = (message, type = 'success') => {
  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.textContent = message;
  toastContainer.appendChild(toast);
  setTimeout(() => toast.remove(), 3500);
};

const updatePreview = () => {
  const imageUrlValue = productImageUrl?.value?.trim();
  if (!productName.value && !productPrice.value && !productStock.value && !productCategory.value && !selectedImageFile && !imageUrlValue) {
    previewCard.classList.add('hidden');
    return;
  }

  previewName.textContent = productName.value || 'Nome do produto';
  previewPrice.textContent = productPrice.value ? `Preço: R$ ${Number(productPrice.value).toFixed(2)}` : 'Preço: -';
  previewStock.textContent = productStock.value ? `Estoque: ${productStock.value}` : 'Estoque: -';
  previewCategory.textContent = productCategory.value ? `Categoria: ${productCategory.value}` : 'Categoria: -';

  if (selectedImageFile) {
    previewImage.src = URL.createObjectURL(selectedImageFile);
  } else if (imageUrlValue) {
    previewImage.src = imageUrlValue;
  } else {
    previewImage.src = '';
  }

  previewCard.classList.remove('hidden');
};

const loadProducts = async () => {
  try {
    const response = await fetch(`${BACKEND_URL}/products`, { credentials: 'same-origin' });
    if (!response.ok) throw new Error(`status ${response.status}`);

    const products = await response.json();
    allProducts = Array.isArray(products) ? products : [];
    currentPage = 1;
    applyFilters();
  } catch (error) {
    console.error('Falha ao carregar produtos:', error);
    showToast('Falha ao carregar produtos. Verifique o backend.', 'error');
  }
};

const loadCategories = async () => {
  try {
    const response = await fetch(`${BACKEND_URL}/products/categories`, { credentials: 'same-origin' });
    if (!response.ok) return;

    const categories = await response.json();
    categorySuggestions.innerHTML = categories
      .map(category => `<option value="${category}"></option>`) .join('');
  } catch (error) {
    console.warn('Falha ao carregar categorias:', error);
  }
};

const loadUsers = async () => {
  try {
    const response = await fetchWithAuth(`${window.location.origin}/api/users`);
    if (!response.ok) throw new Error(`status ${response.status}`);

    const users = await response.json();
    renderUserList(users);
  } catch (error) {
    console.error('Falha ao carregar usuários:', error);
    showToast('Falha ao carregar usuários.', 'error');
  }
};

const renderUserList = (users) => {
  userList.innerHTML = '';
  if (!users.length) {
    userList.innerHTML = '<p>Nenhum usuário cadastrado ainda.</p>';
    return;
  }

  users.forEach(user => {
    const card = document.createElement('div');
    card.className = 'user-card';

    const header = document.createElement('div');
    header.className = 'product-row';
    header.innerHTML = `<h3>${user.fullName || user.email}</h3><span>${user.email}</span>`;

    const details = document.createElement('p');
    details.textContent = `CPF: ${user.cpf || '-'} • Telefone: ${user.phone || '-'} • CEP: ${user.cep || '-'} ${user.address ? '• ' + user.address : ''}`;

    const actions = document.createElement('div');
    actions.className = 'product-row';
    actions.style.gap = '0.5rem';

    const resetButton = document.createElement('button');
    resetButton.textContent = 'Resetar senha';
    resetButton.className = 'btn-primary';
    resetButton.onclick = () => resetUserPassword(user.email);

    const deleteButton = document.createElement('button');
    deleteButton.textContent = 'Excluir';
    deleteButton.className = 'btn-primary';
    deleteButton.style.background = '#dc2626';
    deleteButton.onclick = () => deleteUser(user.email);

    actions.appendChild(resetButton);
    actions.appendChild(deleteButton);

    card.appendChild(header);
    card.appendChild(details);
    card.appendChild(actions);
    userList.appendChild(card);
  });
};

const resetUserPassword = async (email) => {
  const newPasswordValue = prompt(`Informe a nova senha para ${email}:`, '');
  if (!newPasswordValue) return;

  try {
    const response = await fetchWithAuth(`${window.location.origin}/api/users/${encodeURIComponent(email)}/password`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newPassword: newPasswordValue })
    });

    if (!response.ok) {
      const error = await response.json().catch(() => null);
      throw new Error(error?.message || `status ${response.status}`);
    }

    showToast('Senha do usuário atualizada com sucesso.');
    await loadUsers();
  } catch (error) {
    console.error('Erro ao resetar senha do usuário:', error);
    showToast(error.message || 'Erro ao resetar senha.', 'error');
  }
};

const deleteUser = async (email) => {
  if (!confirm(`Deseja realmente excluir o usuário ${email}?`)) return;

  try {
    const response = await fetchWithAuth(`${window.location.origin}/api/users/${encodeURIComponent(email)}`, {
      method: 'DELETE'
    });

    if (!response.ok) {
      const error = await response.json().catch(() => null);
      throw new Error(error?.message || `status ${response.status}`);
    }

    showToast('Usuário excluído com sucesso.');
    await loadUsers();
  } catch (error) {
    console.error('Erro ao excluir usuário:', error);
    showToast(error.message || 'Erro ao excluir usuário.', 'error');
  }
};

const renderProductList = (products = null) => {
  const currentProducts = Array.isArray(products) ? products : getCurrentPageProducts();
  productList.innerHTML = '';
  if (!currentProducts.length) {
    productList.innerHTML = '<p class="empty-msg">Nenhum produto cadastrado ainda.</p>';
    updatePagination();
    return;
  }

  const table = document.createElement('div');
  table.className = 'product-table';

  const header = document.createElement('div');
  header.className = 'product-table-header';
  header.innerHTML = `
    <span class="product-table-cell cell-image">Imagem</span>
    <span class="product-table-cell cell-product">Produto</span>
    <span class="product-table-cell">Categoria</span>
    <span class="product-table-cell">Preço</span>
    <span class="product-table-cell">Estoque</span>
    <span class="product-table-cell cell-actions">Ações</span>
  `;
  table.appendChild(header);

  currentProducts.forEach(product => {
    const name = product.name || '-';
    const price = Number(product.price ?? 0).toFixed(2);
    const category = product.category || '-';
    const stockText = product.stockQuantity ?? product.stock ?? '-';
    const imageUrl = product.imageUrl || 'https://via.placeholder.com/320x200?text=Sem+imagem';
    const desc = product.description || 'Sem descrição disponível.';
    const shortDesc = desc.length > 80 ? `${desc.slice(0, 77)}...` : desc;

    const row = document.createElement('div');
    row.className = 'product-table-row';

    const imageCell = document.createElement('div');
    imageCell.className = 'product-table-cell cell-image';
    const thumb = document.createElement('img');
    thumb.src = imageUrl;
    thumb.alt = name;
    imageCell.appendChild(thumb);

    const productCell = document.createElement('div');
    productCell.className = 'product-table-cell cell-product';
    const title = document.createElement('strong');
    title.textContent = name;
    const info = document.createElement('p');
    info.className = 'product-description';
    info.textContent = shortDesc;
    productCell.appendChild(title);
    productCell.appendChild(info);

    const categoryCell = document.createElement('div');
    categoryCell.className = 'product-table-cell';
    categoryCell.textContent = category;

    const priceCell = document.createElement('div');
    priceCell.className = 'product-table-cell';
    priceCell.textContent = `R$ ${price}`;

    const stockCell = document.createElement('div');
    stockCell.className = 'product-table-cell';
    stockCell.textContent = stockText;

    const actionsCell = document.createElement('div');
    actionsCell.className = 'product-table-cell cell-actions';
    const editButton = document.createElement('button');
    editButton.type = 'button';
    editButton.className = 'btn-primary btn-small';
    editButton.textContent = 'Editar';
    editButton.onclick = () => fillFormForEdit(product);

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'btn-primary btn-danger';
    deleteButton.textContent = 'Remover';
    deleteButton.onclick = () => removeProduct(product.id);

    actionsCell.appendChild(editButton);
    actionsCell.appendChild(deleteButton);

    row.appendChild(imageCell);
    row.appendChild(productCell);
    row.appendChild(categoryCell);
    row.appendChild(priceCell);
    row.appendChild(stockCell);
    row.appendChild(actionsCell);
    table.appendChild(row);
  });

  productList.appendChild(table);
  updatePagination();
};

const fillFormForEdit = (product) => {
  selectedProductId = product.id;
  productName.value = product.name || '';
  productDescription.value = product.description || '';
  productPrice.value = product.price ?? '';
  productStock.value = product.stockQuantity ?? product.stock ?? '';
  productCategory.value = product.category || '';
  selectedImageFile = null;
  previewImage.src = product.imageUrl || '';
  previewName.textContent = product.name || 'Nome do produto';
  previewPrice.textContent = product.price != null ? `Preço: R$ ${Number(product.price).toFixed(2)}` : 'Preço: -';
  const stockValue = product.stockQuantity ?? product.stock;
  previewStock.textContent = stockValue != null && stockValue !== '' ? `Estoque: ${stockValue}` : 'Estoque: -';
  previewCategory.textContent = product.category || 'Categoria: -';
  previewCard.classList.remove('hidden');
};

const removeProduct = async (id) => {
  if (!confirm('Tem certeza que deseja remover este produto?')) return;

  try {
    const response = await fetchWithAuth(`${BACKEND_URL}/products/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) {
      if (response.status === 401 || response.status === 403) {
        setLoggedOutState();
        throw new Error('Acesso não autorizado. Faça login novamente.');
      }
      const err = await response.json().catch(() => null);
      throw new Error(err?.message || `status ${response.status}`);
    }
    showToast('Produto removido com sucesso.');
    loadProducts();
  } catch (error) {
    console.error('Erro ao remover produto:', error);
    showToast(error.message || 'Erro ao remover produto.', 'error');
  }
};

const handleFileChange = (event) => {
  const file = event.target.files[0];
  if (!file) {
    selectedImageFile = null;
    updatePreview();
    return;
  }

  selectedImageFile = file;
  updatePreview();
};

const setLoggedInState = (email) => {
  loginSection.classList.add('hidden');
  adminAppSection.classList.remove('hidden');
  adminUserActions.classList.remove('hidden');
  adminUserEmail.textContent = email;
};

const setLoggedOutState = () => {
  loginSection.classList.remove('hidden');
  adminAppSection.classList.add('hidden');
  adminUserActions.classList.add('hidden');
  adminUserEmail.textContent = '';
  clearAuthStorage();
};

const refreshAccessToken = async () => {
  try {
    const response = await fetch(window.location.origin + '/api/users/refresh-token', {
      method: 'POST',
      credentials: 'same-origin'
    });

    if (!response.ok) return false;
    const result = await response.json();
    setAuthToken(result.token);
    return true;
  } catch {
    return false;
  }
};

const fetchWithAuth = async (url, options = {}) => {
  options.headers = { ...(options.headers || {}), ...authHeaders() };
  options.credentials = 'same-origin';

  let response = await fetch(url, options);
  if (response.status === 401) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      options.headers = { ...(options.headers || {}), ...authHeaders() };
      response = await fetch(url, options);
    }
  }

  return response;
};

const login = async (event) => {
  event.preventDefault();

  const email = loginEmail.value.trim();
  const password = loginPassword.value;

  if (!email || !password) {
    showToast('E-mail e senha são obrigatórios.', 'warning');
    return;
  }

  try {
    const response = await fetch(window.location.origin + '/api/users/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
      body: JSON.stringify({ email, password })
    });

    const result = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(result?.message || `status ${response.status}`);
    }

    setAuthToken(result.token);
    setAuthEmail(result.email);
    setLoggedInState(result.email);
    loginForm.reset();
    await loadProducts();
    await loadCategories();
    await loadUsers();
    showToast('Login realizado com sucesso.');
  } catch (error) {
    console.error('Erro ao efetuar login:', error);
    showToast(error.message || 'Falha no login.', 'error');
  }
};

const logout = async () => {
  try {
    await fetch(window.location.origin + '/api/users/logout', {
      method: 'POST',
      credentials: 'same-origin'
    });
  } catch (error) {
    console.warn('Erro ao chamar logout:', error);
  }

  setLoggedOutState();
  showToast('Você saiu do admin.', 'info');
};

const initializeSession = async () => {
  const storedEmail = localStorage.getItem(AUTH_EMAIL_KEY);

  if (getAuthToken() && storedEmail) {
    setLoggedInState(storedEmail);
    return true;
  }

  const refreshed = await refreshAccessToken();
  if (refreshed && storedEmail) {
    setLoggedInState(storedEmail);
    return true;
  }

  setLoggedOutState();
  return false;
};

const handleUserRegister = async (event) => {
  event.preventDefault();

  const email = userEmail.value.trim();
  const fullName = userFullName.value.trim();
  const passwordValue = userPassword.value;
  const cpfValue = userCpf.value.trim();
  const phoneValue = userPhone.value.trim();
  const cepValue = userCep.value.trim();
  const addressValue = userAddress.value.trim();

  if (!email || !fullName || !passwordValue) {
    showToast('E-mail, nome e senha são obrigatórios.', 'warning');
    return;
  }

  try {
    const response = await fetchWithAuth(window.location.origin + '/api/users/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email,
        fullName,
        password: passwordValue,
        cpf: cpfValue,
        phone: phoneValue,
        cep: cepValue,
        address: addressValue
      })
    });

    const result = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(result?.message || `status ${response.status}`);
    }

    userRegisterForm.reset();
    showToast('Usuário criado com sucesso.');
    await loadUsers();
  } catch (error) {
    console.error('Erro ao criar usuário:', error);
    showToast(error.message || 'Erro ao criar usuário.', 'error');
  }
};

const handleChangePassword = async (event) => {
  event.preventDefault();

  const currentPwd = currentPassword.value;
  const newPwd = newPassword.value;
  const userEmailStored = localStorage.getItem(AUTH_EMAIL_KEY);

  if (!currentPwd || !newPwd) {
    showToast('Preencha as senhas corretamente.', 'warning');
    return;
  }

  if (!userEmailStored) {
    showToast('Usuário não identificado.', 'error');
    return;
  }

  try {
    const response = await fetchWithAuth(`${window.location.origin}/api/users/${encodeURIComponent(userEmailStored)}/password`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ currentPassword: currentPwd, newPassword: newPwd })
    });

    const result = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(result?.message || `status ${response.status}`);
    }

    changePasswordForm.reset();
    showToast('Senha alterada com sucesso.');
  } catch (error) {
    console.error('Erro ao alterar senha:', error);
    showToast(error.message || 'Erro ao alterar senha.', 'error');
  }
};

const handleProductSubmit = async (event) => {
  event.preventDefault();

  const name = productName.value.trim();
  const description = productDescription.value.trim();
  const category = productCategory.value.trim();
  const price = Number(productPrice.value);
  const stock = Number(productStock.value);

  if (!name || !category || !price || !Number.isFinite(price) || price < 0 || !Number.isInteger(stock) || stock < 0) {
    showToast('Preencha corretamente todos os campos obrigatórios.', 'warning');
    return;
  }

  const imageUrlValue = productImageUrl.value.trim();
  if (!selectedImageFile && !selectedProductId && !imageUrlValue) {
    showToast('Selecione uma imagem do produto ou forneça uma URL.', 'warning');
    return;
  }

  const formData = new FormData();
  formData.append('Name', name);
  formData.append('Description', description);
  formData.append('Category', category);
  formData.append('Price', price);
  formData.append('StockQuantity', stock);

  if (selectedImageFile) {
    formData.append('Image', selectedImageFile);
  } else if (imageUrlValue) {
    formData.append('ImageUrl', imageUrlValue);
  }

  try {
    const method = selectedProductId ? 'PUT' : 'POST';
    const url = selectedProductId ? `${BACKEND_URL}/products/${selectedProductId}` : `${BACKEND_URL}/products`;

    const response = await fetchWithAuth(url, {
      method,
      body: formData
    });
    const result = await response.json().catch(() => null);

    if (!response.ok) {
      throw new Error(result?.message || `status ${response.status}`);
    }

    showToast(selectedProductId ? 'Produto atualizado.' : 'Produto cadastrado.');
    productForm.reset();
    selectedProductId = null;
    selectedImageFile = null;
    previewCard.classList.add('hidden');
    await loadProducts();
    await loadCategoriesForFilter();
  } catch (error) {
    console.error('Erro ao salvar produto:', error);
    showToast(error.message || 'Erro ao salvar produto.', 'error');
  }
};

productImage.addEventListener('change', handleFileChange);
productForm.addEventListener('submit', handleProductSubmit);
productName.addEventListener('input', updatePreview);
productDescription.addEventListener('input', updatePreview);
productPrice.addEventListener('input', updatePreview);
productStock.addEventListener('input', updatePreview);
productCategory.addEventListener('input', updatePreview);
productImageUrl.addEventListener('input', updatePreview);
loginForm.addEventListener('submit', login);
logoutButton.addEventListener('click', logout);
userRegisterForm.addEventListener('submit', handleUserRegister);
changePasswordForm.addEventListener('submit', handleChangePassword);

const menuButtons = document.querySelectorAll('.menu-button');
const tabSections = document.querySelectorAll('.admin-tab-section');
const productSearch = document.getElementById('product-search');
const categoryPillList = document.getElementById('category-pill-list');
const productPagination = document.getElementById('product-pagination');
const prevPageButton = document.getElementById('prev-page');
const nextPageButton = document.getElementById('next-page');
const pageInfo = document.getElementById('page-info');

const setActiveTab = (tabId) => {
  menuButtons.forEach(button => {
    const isActive = button.dataset.tab === tabId;
    button.classList.toggle('active', isActive);
  });

  tabSections.forEach(section => {
    section.classList.toggle('hidden', section.id !== tabId);
  });
};

menuButtons.forEach(button => {
  button.addEventListener('click', () => {
    setActiveTab(button.dataset.tab);
  });
});

const buildCategoryPills = (categories) => {
  const uniqueCategories = Array.from(new Set(categories.map(c => c?.trim()).filter(Boolean)));
  categoryPillList.innerHTML = '';

  const allPill = document.createElement('button');
  allPill.type = 'button';
  allPill.className = 'category-pill active';
  allPill.textContent = 'Todas';
  allPill.dataset.category = '';
  categoryPillList.appendChild(allPill);

  uniqueCategories.forEach(category => {
    const pill = document.createElement('button');
    pill.type = 'button';
    pill.className = 'category-pill';
    pill.textContent = category;
    pill.dataset.category = category;
    categoryPillList.appendChild(pill);
  });

  categoryPillList.querySelectorAll('.category-pill').forEach(pill => {
    pill.addEventListener('click', () => {
      selectedCategoryFilter = pill.dataset.category;
      categoryPillList.querySelectorAll('.category-pill').forEach(button => button.classList.toggle('active', button === pill));
      currentPage = 1;
      applyFilters();
    });
  });
};

const applyFilters = () => {
  const term = searchQuery.trim().toLowerCase();
  filteredProducts = allProducts.filter(product => {
    const name = (product.name || product.Name || '').toLowerCase();
    const category = (product.category || product.Category || '').toLowerCase();
    const description = (product.description || product.Description || '').toLowerCase();

    const matchesSearch = !term || name.includes(term) || category.includes(term) || description.includes(term);
    const matchesCategory = !selectedCategoryFilter || category === selectedCategoryFilter.toLowerCase();
    return matchesSearch && matchesCategory;
  });

  const totalPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));
  if (currentPage > totalPages) currentPage = totalPages;
  renderProductList();
};

const getCurrentPageProducts = () => {
  const start = (currentPage - 1) * PAGE_SIZE;
  return filteredProducts.slice(start, start + PAGE_SIZE);
};

const updatePagination = () => {
  const totalPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));
  if (currentPage > totalPages) currentPage = totalPages;
  pageInfo.textContent = `Página ${currentPage} de ${totalPages}`;
  productPagination.classList.toggle('hidden', totalPages <= 1);
  prevPageButton.disabled = currentPage <= 1;
  nextPageButton.disabled = currentPage >= totalPages;
};

if (productSearch) {
  productSearch.addEventListener('input', (event) => {
    searchQuery = event.target.value;
    currentPage = 1;
    applyFilters();
  });
}

if (prevPageButton) {
  prevPageButton.addEventListener('click', () => {
    if (currentPage > 1) {
      currentPage -= 1;
      renderProductList();
    }
  });
}

if (nextPageButton) {
  nextPageButton.addEventListener('click', () => {
    const totalPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));
    if (currentPage < totalPages) {
      currentPage += 1;
      renderProductList();
    }
  });
}

const buildCategoryFiltersFromProducts = () => {
  const categories = Array.from(new Set(
    allProducts
      .map(p => p.category || p.Category)
      .filter(Boolean)
      .map(category => String(category).trim())
  )).sort((a, b) => a.localeCompare(b, 'pt-BR', { sensitivity: 'base' }));
  buildCategoryPills(categories);
};

const loadCategoriesForFilter = async () => {
  try {
    const response = await fetch(`${BACKEND_URL}/products/categories`);
    if (!response.ok) {
      buildCategoryFiltersFromProducts();
      return;
    }

    const categories = await response.json();
    if (!categories || !categories.length) {
      buildCategoryFiltersFromProducts();
      return;
    }

    categorySuggestions.innerHTML = categories.map(category => `<option value="${category}"></option>`).join('');
    buildCategoryPills(categories);
  } catch (error) {
    console.warn('Falha ao carregar categorias para filtro:', error);
    buildCategoryFiltersFromProducts();
  }
};

setActiveTab('products-section');

initializeSession().then(async (loggedIn) => {
  if (loggedIn) {
    await loadProducts();
    await loadCategories();
    await loadCategoriesForFilter();
    await loadUsers();
  }
});
