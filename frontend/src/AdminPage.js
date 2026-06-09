import React, { useState, useEffect } from 'react';
import './AdminDashboard.css';
import ProductDetailModal from './ProductDetailModal';
import { apiUrl } from './api';

function resolveAssetUrl(url) {
  if (!url) {
    return '';
  }

  if (/^(https?:|data:|blob:)/i.test(url)) {
    return url;
  }

  return apiUrl(url.startsWith('/') ? url : `/${url}`);
}

function AdminPanel({ user, onLogout }) {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [productImages, setProductImages] = useState([]);
  const [selectedImageUrl, setSelectedImageUrl] = useState('');
  const [draggingImageUrl, setDraggingImageUrl] = useState('');
  const [assigningProductId, setAssigningProductId] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    brand: '',
    basePrice: '',
    categoryId: '',
    isActive: true,
    stockQuantity: 0,
    colorName: '',
    sizeName: ''
  });
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [activeTab, setActiveTab] = useState('add');
  const [selectedProduct, setSelectedProduct] = useState(null);

  // search
  const [searchQuery, setSearchQuery] = useState("");



  // Fetch categories and products
  useEffect(() => {
    fetchCategories();
    fetchProducts();
    fetchProductImages();
  }, []);

  const fetchCategories = async () => {
    try {
      const response = await fetch(apiUrl('/debug/categories'));
      const data = await response.json();
      setCategories(data.categories);
    } catch (err) {
      console.error('Failed to fetch categories:', err);
    }
  };

  const fetchProducts = async () => {
    try {
      const response = await fetch(apiUrl('/products'));
      const data = await response.json();
      setProducts(data);
    } catch (err) {
      console.error('Failed to fetch products:', err);
    }
  };

  const fetchProductImages = async () => {
    try {
      const response = await fetch(apiUrl('/admin/product-images'));
      const data = await response.json();
      setProductImages(data);
    } catch (err) {
      console.error('Failed to fetch product images:', err);
    }
  };

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');

    if (!formData.name || !formData.categoryId || !formData.basePrice) {
      setMessage('Please fill in all required fields');
      setLoading(false);
      return;
    }

    try {
      const url = editingId 
        ? apiUrl(`/products/${editingId}`)
        : apiUrl('/products');
      
      const method = editingId ? 'PUT' : 'POST';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: formData.name,
          description: formData.description,
          brand: formData.brand,
          basePrice: parseFloat(formData.basePrice),
          categoryId: parseInt(formData.categoryId),
          isActive: formData.isActive,
          stockQuantity: parseInt(formData.stockQuantity, 10) || 0,
          colorName: formData.colorName.trim(),
          sizeName: formData.sizeName.trim()
        })
      });

      if (response.ok) {
        setFormData({
          name: '',
          description: '',
          brand: '',
          basePrice: '',
          categoryId: '',
          isActive: true,
          stockQuantity: 0,
          colorName: '',
          sizeName: ''
        });
        setEditingId(null);
        setActiveTab('list');
        fetchProducts(); // Refresh product list
      } else {
        setMessage('Failed to save product');
      }
    } catch (err) {
      setMessage('Error: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleEditProduct = (product) => {
    setFormData({
      name: product.name,
      description: product.description,
      brand: product.brand,
      basePrice: product.basePrice,
      categoryId: product.categoryId,
      isActive: product.isActive,
      stockQuantity: product.stockQuantity,
      colorName: product.colorName || '',
      sizeName: product.sizeName || ''
    });
    setEditingId(product.productId);
    setActiveTab('add');
    setMessage('');
  };

  const handleCancelEdit = () => {
    setFormData({
      name: '',
      description: '',
      brand: '',
      basePrice: '',
      categoryId: '',
      isActive: true,
      stockQuantity: 0,
      colorName: '',
      sizeName: ''
    });
    setEditingId(null);
    setMessage('');
  };

  const handleDeleteProduct = async (productId) => {
    if (window.confirm('Are you sure you want to delete this product?')) {
      try {
        const response = await fetch(apiUrl(`/products/${productId}`), {
          method: 'DELETE'
        });

        if (response.ok) {
          setMessage('Product deleted successfully!');
          fetchProducts();
        } else {
          setMessage('Failed to delete product');
        }
      } catch (err) {
        setMessage('Error: ' + err.message);
      }
    }
  };

  const toggleProductStatus = async (product) => {
    try {
      const response = await fetch(apiUrl(`/products/${product.productId}`), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: product.name,
          description: product.description,
          brand: product.brand,
          basePrice: product.basePrice,
          categoryId: product.categoryId,
          isActive: !product.isActive,
          stockQuantity: product.stockQuantity
        })
      });

      if (response.ok) {
        const newStatus = !product.isActive ? 'Active' : 'Inactive';
        setMessage(`Product "${product.name}" is now ${newStatus}!`);
        fetchProducts();
      } else {
        setMessage('Failed to update product status');
      }
    } catch (err) {
      setMessage('Error: ' + err.message);
    }
  };

  const handleAssignImage = async (product, imageUrl = selectedImageUrl) => {
    if (!imageUrl) {
      setMessage('Select an image first');
      return;
    }

    setAssigningProductId(product.productId);
    setMessage('');

    try {
      const response = await fetch(apiUrl(`/products/${product.productId}/image`), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ imageUrl })
      });

      if (response.ok) {
        setMessage(`Image linked to "${product.name}"`);
        await fetchProducts();
      } else {
        setMessage('Failed to link image');
      }
    } catch (err) {
      setMessage('Error: ' + err.message);
    } finally {
      setAssigningProductId(null);
    }
  };

  const handleProductImageDrop = (event, product) => {
    event.preventDefault();
    event.stopPropagation();

    const droppedImageUrl = event.dataTransfer.getData('text/plain') || draggingImageUrl;
    handleAssignImage(product, droppedImageUrl);
    setDraggingImageUrl('');
  };

  const getCategoryName = (categoryId) => {
    const cat = categories.find(c => c.categoryId === categoryId);
    return cat ? cat.name : 'Unknown';
  };

  const isErrorMessage = message.startsWith('Failed') ||
    message.startsWith('Error') ||
    message.startsWith('Please');

  
  // search
    const normalizedSearch = searchQuery.trim().toLowerCase();

    const filteredProducts = products.filter(product => {
      if (!normalizedSearch) return true;

      return (product.name?.toLowerCase().includes(normalizedSearch) ||product.brand?.toLowerCase().includes(normalizedSearch) ||product.categoryName?.toLowerCase().includes(normalizedSearch));
    });

  return (
    <div className="admin-panel">
      {/* HEADER */}
      <header className="admin-header">
        <div className="admin-header-top">
          <h1>Admin Dashboard</h1>
          <div className="admin-user-info">
            <span>Welcome, <strong>{user?.username}</strong></span>
            <button className="logout-btn" onClick={onLogout}>Logout</button>
          </div>
        </div>

        <div className="admin-tabs">
          <button
            className={`tab-btn ${activeTab === 'add' ? 'active' : ''}`}
            onClick={() => {
              setActiveTab('add');
              handleCancelEdit();
            }}
          >
            {editingId ? 'Edit Product' : '+ Add Product'}
          </button>
          <button
            className={`tab-btn ${activeTab === 'list' ? 'active' : ''}`}
            onClick={() => setActiveTab('list')}
          >
            Products ({products.length})
          </button>
          <button
            className={`tab-btn ${activeTab === 'images' ? 'active' : ''}`}
            onClick={() => setActiveTab('images')}
          >
            Images ({productImages.length})
          </button>
        </div>
      </header>

      {/* MAIN */}
      <main className="admin-main">
        {/* ADD/EDIT PRODUCT TAB */}
        {activeTab === 'add' && (
          <section className="admin-section add-product-section">
            <h2>{editingId ? 'Edit Product' : 'Add New Product'}</h2>

            <form onSubmit={handleSubmit} className="product-form">
              <div className="form-group">
                <label htmlFor="name">Product Name *</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  value={formData.name}
                  onChange={handleInputChange}
                  placeholder="e.g., Cotton T-Shirt"
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="brand">Brand</label>
                <input
                  type="text"
                  id="brand"
                  name="brand"
                  value={formData.brand}
                  onChange={handleInputChange}
                  placeholder="e.g., ComfortWear"
                />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="basePrice">Price (€) *</label>
                  <input
                    type="number"
                    id="basePrice"
                    name="basePrice"
                    value={formData.basePrice}
                    onChange={handleInputChange}
                    placeholder="29.99"
                    step="0.01"
                    min="0"
                    required
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="stockQuantity">Stock Quantity *</label>
                  <input
                    type="number"
                    id="stockQuantity"
                    name="stockQuantity"
                    value={formData.stockQuantity}
                    onChange={handleInputChange}
                    min="0"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="categoryId">Category *</label>
                  <select
                    id="categoryId"
                    name="categoryId"
                    value={formData.categoryId}
                    onChange={handleInputChange}
                    required
                  >
                    <option value="">Select a category</option>
                    {categories
                      .filter(cat => cat.name.toLowerCase() !== 'home')
                      .map(cat => (
                      <option key={cat.categoryId} value={cat.categoryId}>
                        {cat.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="description">Description</label>
                <textarea
                  id="description"
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  placeholder="Product description..."
                  rows="4"
                />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="colorName">Color</label>
                  <input
                    type="text"
                    id="colorName"
                    name="colorName"
                    value={formData.colorName}
                    onChange={handleInputChange}
                    placeholder="e.g., Black"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="sizeName">Size</label>
                  <input
                    type="text"
                    id="sizeName"
                    name="sizeName"
                    value={formData.sizeName}
                    onChange={handleInputChange}
                    placeholder="e.g., M"
                  />
                </div>
              </div>

              <div className="form-group checkbox">
                <input
                  type="checkbox"
                  id="isActive"
                  name="isActive"
                  checked={formData.isActive}
                  onChange={handleInputChange}
                />
                <label htmlFor="isActive">Active (visible to customers)</label>
              </div>

              <div className="form-actions">
                <button
                  type="submit"
                  className="submit-btn"
                  disabled={loading}
                >
                  {loading ? 'Saving...' : editingId ? '✔ Update Product' : '✔ Create Product'}
                </button>

                {editingId && (
                  <button
                    type="button"
                    className="cancel-btn"
                    onClick={handleCancelEdit}
                  >
                    ✕ Cancel
                  </button>
                )}
              </div>
            </form>
          </section>
        )}

        {/* PRODUCTS LIST TAB */}
        {activeTab === 'list' && (
          <section className="admin-section products-list-section">
            <h2>All Products</h2>
            <input
              type="text"
              placeholder="Search products..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
            />

            {products.length === 0 ? (
              <p className="no-products">No products yet. Add one to get started!</p>
            ) : (
              <div className="products-table-wrapper">
                <table className="products-table">
                  <thead>
                    <tr>
                      <th>Image</th>
                      <th>Name</th>
                      <th>Brand</th>
                      <th>Category</th>
                      <th>Variant</th>
                      <th>Price</th>
                      <th>Stock</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredProducts.map(product => (
                      <tr
                        key={product.productId}
                        className="product-row"
                        onClick={() => setSelectedProduct(product)}
                      >
                        <td>
                          <div className="product-table-image">
                            {product.imageUrl ? (
                              <img src={resolveAssetUrl(product.imageUrl)} alt={product.name} />
                            ) : (
                              <span>No image</span>
                            )}
                          </div>
                        </td>
                        <td className="product-name">{product.name}</td>
                        <td>{product.brand}</td>
                        <td>
                          <span className="category-badge">
                            {getCategoryName(product.categoryId)}
                          </span>
                        </td>
                        <td>
                          <span className="variant-summary">
                            {[product.colorName, product.sizeName].filter(Boolean).join(' / ') || 'default'}
                          </span>
                        </td>
                        <td className="price">€ {Number(product.basePrice).toFixed(2)}</td>
                        <td>
                          <span className={`stock ${product.stockQuantity === 0 ? 'out' : ''}`}>
                            {product.stockQuantity}
                          </span>
                        </td>
                        <td>
                          <button
                            className={`status-btn ${product.isActive ? 'active' : 'inactive'}`}
                            onClick={(event) => {
                              event.stopPropagation();
                              toggleProductStatus(product);
                            }}
                            title={product.isActive ? 'Click to deactivate' : 'Click to activate'}
                          >
                            {product.isActive ? '✓ Active' : '✗ Inactive'}
                          </button>
                        </td>
                        <td className="actions-cell">
                          <button
                            className="edit-btn"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleEditProduct(product);
                            }}
                            title="Edit product"
                          >
                            ✏️
                          </button>
                          <button
                            className="delete-btn"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleDeleteProduct(product.productId);
                            }}
                            title="Delete product"
                          >
                            🗑️
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        )}

        {activeTab === 'images' && (
          <section className="admin-section image-manager-section">
            <div className="section-heading-row">
              <h2>Product Images</h2>
              <button type="button" className="cancel-btn small" onClick={fetchProductImages}>
                Refresh
              </button>
            </div>

            <div className="image-manager-layout">
              <div className="image-gallery">
                {productImages.length === 0 ? (
                  <p className="no-products">No images found in backend/ProductImages.</p>
                ) : (
                  productImages.map((image) => (
                    <button
                      key={image.url}
                      type="button"
                      className={`image-tile ${selectedImageUrl === image.url ? 'selected' : ''}`}
                      draggable
                      onClick={() => setSelectedImageUrl(image.url)}
                      onDragStart={(event) => {
                        setDraggingImageUrl(image.url);
                        event.dataTransfer.setData('text/plain', image.url);
                      }}
                      onDragEnd={() => setDraggingImageUrl('')}
                    >
                      <img src={resolveAssetUrl(image.url)} alt={image.displayName || image.fileName} />
                      <span>{image.fileName}</span>
                    </button>
                  ))
                )}
              </div>

              <div className="image-product-list">
                {products.map((product) => (
                  <div
                    key={product.productId}
                    className="image-product-row"
                    onDragOver={(event) => event.preventDefault()}
                    onDrop={(event) => handleProductImageDrop(event, product)}
                  >
                    <div className="product-table-image">
                      {product.imageUrl ? (
                        <img src={resolveAssetUrl(product.imageUrl)} alt={product.name} />
                      ) : (
                        <span>No image</span>
                      )}
                    </div>
                    <div className="image-product-meta">
                      <strong>{product.name}</strong>
                      <span>{product.brand || getCategoryName(product.categoryId)}</span>
                    </div>
                    <button
                      type="button"
                      className="submit-btn small"
                      disabled={!selectedImageUrl || assigningProductId === product.productId}
                      onClick={() => handleAssignImage(product)}
                    >
                      {assigningProductId === product.productId ? 'Linking...' : 'Assign'}
                    </button>
                  </div>
                ))}
              </div>
            </div>
          </section>
        )}
      </main>

      {/* MESSAGE */}
      {message && (
        <div className={`message ${isErrorMessage ? 'error' : 'success'}`} role="status">
          {message}
        </div>
      )}

      <ProductDetailModal
        product={selectedProduct}
        isOpen={Boolean(selectedProduct)}
        onClose={() => setSelectedProduct(null)}
        user={user}
      />

      {/* FOOTER */}
      <footer className="admin-footer">
        <p>Admin Panel v1.0 | Manage your store inventory</p>
      </footer>
    </div>
  );
}

export default AdminPanel;
