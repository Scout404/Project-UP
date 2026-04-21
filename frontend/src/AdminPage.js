import React, { useState, useEffect } from 'react';
import './AdminDashboard.css';

const defaultInventory = [
  { id: 1, name: 'Shirt', price: '20', stock: '50' },
  { id: 2, name: 'Pants', price: '50', stock: '20' }
];

function AdminPage({ user, onLogout }) {
  const [items, setItems] = useState(() => {
    const saved = localStorage.getItem('adminInventory');
    const parsed = saved ? JSON.parse(saved) : defaultInventory;
    return parsed.map(item => ({
      ...item,
      price: item.price?.toString() ?? '0',
      stock: item.stock?.toString() ?? '0'
    }));
  });

  useEffect(() => {
    localStorage.setItem('adminInventory', JSON.stringify(items));
  }, [items]);

  const handleChange = (id, field, value) => {
    setItems(items.map(item =>
      item.id === id ? { ...item, [field]: value } : item
    ));
  };

  const addItem = () => {
    setItems([
      ...items,
      {
        id: Date.now(),
        name: 'New Item',
        price: '0',
        stock: '0'
      }
    ]);
  };

  const removeItem = (id) => {
    setItems(items.filter(item => item.id !== id));
  };

  const totalStock = items.reduce((sum, item) => sum + Number(item.stock || 0), 0);
  const totalItems = items.length;

  return (
    <div className="admin-dashboard">
      <header className="admin-header">
        <div>
          <p className="eyebrow">Admin page</p>
          <h1>Product management</h1>
          <p className="subtitle">
            Add, update, or delete inventory items.
          </p>
        </div>

        <div className="admin-actions">
          <p className="admin-user">Logged in as {user.username}</p>
          <button className="logout-btn" onClick={onLogout}>Logout</button>
          <button className="primary-btn" onClick={addItem}>+ New product</button>
        </div>
      </header>

      <section className="stats-grid">
        <div className="stat-card">
          <p className="stat-label">Total items</p>
          <p className="stat-value">{totalItems}</p>
        </div>
        <div className="stat-card">
          <p className="stat-label">Total stock</p>
          <p className="stat-value">{totalStock}</p>
        </div>
      </section>

      <section className="inventory-panel">
        <div className="panel-head">
          <div>
            <h2>Inventory list</h2>
            <p>Update each row inline, then delete items as needed.</p>
          </div>
        </div>

        <div className="table-frame">
          <table className="inventory-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Price (€)</th>
                <th>Stock</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td>
                    <input
                      className="table-input"
                      type="text"
                      value={item.name}
                      onChange={(e) => handleChange(item.id, 'name', e.target.value)}
                    />
                  </td>
                  <td>
                    <input
                      className="table-input"
                      type="number"
                      min="0"
                      value={item.price}
                      onChange={(e) => handleChange(item.id, 'price', e.target.value)}
                    />
                  </td>
                  <td>
                    <input
                      className="table-input"
                      type="number"
                      min="0"
                      value={item.stock}
                      onChange={(e) => handleChange(item.id, 'stock', e.target.value)}
                    />
                  </td>
                  <td>
                    <button className="danger-btn" onClick={() => removeItem(item.id)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}

export default AdminPage;
