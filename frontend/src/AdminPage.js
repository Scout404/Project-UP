import React, { useState, useEffect } from 'react';
import './AdminDashboard.css';

function AdminPage({ user, onLogout }) {
  const [items, setItems] = useState([]);

  useEffect(() => {
    fetch("http://localhost:5250/products")
      .then(res => res.json())
      .then(data => {
        setItems(data);
      })
      .catch(err => console.error("Load error:", err));
  }, []);

  const addItem = async () => {
    const newItem = {
      name: "New Item",
      basePrice: 0,
      stock: 0,
      category: "Clothes"
    };

    try {
      const res = await fetch("http://localhost:5250/products", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newItem)
      });

      if (!res.ok) return;

      const updated = await fetch("http://localhost:5250/products");
      const data = await updated.json();
      setItems(data);

    } catch (err) {
      console.error("Add item error:", err);
    }
  };

  const saveItem = async (item) => {
    try {
      await fetch(`http://localhost:5250/products/${item.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(item)
      });
    } catch (err) {
      console.error("Save failed:", err);
    }
  };

  const handleChange = (id, field, value) => {
    setItems(prev =>
      prev.map(item =>
        item.id === id ? { ...item, [field]: value } : item
      )
    );
  };

  const removeItem = async (id) => {
    try {
      await fetch(`http://localhost:5250/products/${id}`, {
        method: "DELETE"
      });

      setItems(prev => prev.filter(i => i.id !== id));
    } catch (err) {
      console.error("Delete failed:", err);
    }
  };

  return (
    <div className="admin-dashboard">

      {/* HEADER */}
      <header className="admin-header">
        <div>
          <p className="eyebrow">Admin page</p>
          <h1>Product management</h1>
        </div>

        <div className="admin-actions">
          <p>Logged in as {user.username}</p>

          <button onClick={onLogout}>Logout</button>

          <button className="primary-btn" onClick={addItem}>
            + New product
          </button>
        </div>
      </header>

      {/* STATS */}
      <section className="stats-grid">
        <div className="stat-card">
          <p>Total items</p>
          <h2>{items.length}</h2>
        </div>
      </section>

      {/* PRODUCT LIST */}
      <section className="inventory-list">
        {items.length === 0 ? (
          <p>No products found.</p>
        ) : (
          items.map(item => (
            <div key={item.id} className="product-row">

              <div className="field">
                <label>Name</label>
                <input
                  value={item.name || ""}
                  onChange={(e) =>
                    handleChange(item.id, "name", e.target.value)
                  }
                />
              </div>

              <div className="field">
                <label>Price</label>
                <input
                  type="number"
                  value={item.basePrice ?? 0}
                  onChange={(e) =>
                    handleChange(item.id, "basePrice", Number(e.target.value))
                  }
                />
              </div>

              <div className="field">
                <label>Stock</label>
                <input
                  type="number"
                  value={item.stock ?? 0}
                  onChange={(e) =>
                    handleChange(item.id, "stock", Number(e.target.value))
                  }
                />
              </div>

              <div className="actions">
                <button
                  className="save-btn"
                  onClick={() => saveItem(item)}
                >
                  Save
                </button>

                <button
                  className="delete-btn"
                  onClick={() => removeItem(item.id)}
                >
                  Delete
                </button>
              </div>

            </div>
          ))
        )}
      </section>

    </div>
  );
}

export default AdminPage;