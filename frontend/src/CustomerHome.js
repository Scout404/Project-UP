import React, { useState, useEffect } from 'react';
import './CustomerHome.css';
import LoginModal from './LoginModal';
import { useCart } from './CartContext';

function CustomerHome({ user, onLogout, onLoginSuccess }) {
  const categories = ['home', 'clothes', 'accessoires', 'collections'];
  const links = ['contact', 'about us', 'support'];

  const { cart, addToCart, removeFromCart } = useCart();

  const [isCartOpen, setIsCartOpen] = useState(false);
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false);
  const [products, setProducts] = useState([]);
  const [activePage, setActivePage] = useState("home");

  useEffect(() => {
    fetch("http://localhost:5050/products")
      .then(res => res.json())
      .then(setProducts)
      .catch(err => console.error("Failed to fetch products:", err));
  }, []);

  const filteredProducts = products.filter(
    p => p.categoryName?.toLowerCase() === activePage && p.isActive
  );

  const totalPrice =
    cart?.items?.reduce(
      (sum, item) => sum + (Number(item.price) || 0) * item.quantity,
      0
    ) || 0;

  const handleLoginSuccess = (userData) => {
    setIsLoginModalOpen(false);
    onLoginSuccess(userData);
  };

  return (
    <div className="customer-home">

      {/* HEADER */}
      <header className="customer-header">
        <nav className="customer-nav">

          <ul className="customer-menu">
            {categories.map((category) => (
              <li
                key={category}
                onClick={() => setActivePage(category)}
                style={{
                  cursor: "pointer",
                  fontWeight: activePage === category ? "bold" : "normal",
                  borderBottom: activePage === category ? "2px solid black" : "none"
                }}
              >
                {category}
              </li>
            ))}
          </ul>

          <div className="customer-actions">
            <button className="icon-btn">🔍</button>
            <button className="icon-btn">❤️</button>

            <button
              className="icon-btn"
              onClick={() => setIsCartOpen(true)}
            >
              🛒
            </button>

            {/* Account button - opens login modal if guest, shows user menu if logged in */}
            {!user ? (
              <button
                className="icon-btn"
                onClick={() => setIsLoginModalOpen(true)}
                title="Login or create account"
              >
                👤
              </button>
            ) : (
              <button className="icon-btn" title={`Logged in as ${user.username}`}>
                👤
              </button>
            )}

            {/* Logout button - only visible if logged in */}
            {user && (
              <button className="logout-btn" onClick={onLogout}>
                Logout
              </button>
            )}

            {/* Login button - only visible if guest */}
            {!user && (
              <button 
                className="logout-btn" 
                onClick={() => setIsLoginModalOpen(true)}
              >
                Login
              </button>
            )}
          </div>

        </nav>
      </header>

      {/* LOGIN MODAL */}
      <LoginModal
        isOpen={isLoginModalOpen}
        onClose={() => setIsLoginModalOpen(false)}
        onLoginSuccess={handleLoginSuccess}
      />

      {/* MAIN */}
      <main className="customer-main">

        {/* HOME */}
        {activePage === "home" && (
          <section className="hero-panel">
            <div className="hero-copy">
              <p className="eyebrow">New look</p>
              <h1>Discover new looks.</h1>
              <p className="hero-text">
                Explore latest collection.
              </p>
            </div>

            <div className="hero-visual">
              <div className="hero-image">
                <span>image</span>
              </div>
            </div>
          </section>
        )}

        {/* PRODUCTS */}
        {activePage !== "home" && (
          <section className="products-grid">

            <h2>
              {activePage.charAt(0).toUpperCase() + activePage.slice(1)}
            </h2>

            {filteredProducts.length === 0 ? (
              <p>No items found.</p>
            ) : (
              filteredProducts.map(p => {
                const id = p.productId; 
                const price = p.basePrice || 0;

                return (
                  <div key={id} className="product-card">
                    <h3>{p.name}</h3>
                    <p className="brand-tag">{p.brand}</p>
                    <p className="price-tag">€ {Number(price).toFixed(2)}</p>

                    <button
                      onClick={() => {
                        addToCart({
                          variantId: id,
                          name: p.name,
                          price: Number(price),
                          quantity: 1
                        });
                      }}
                    >
                      Add to cart
                    </button>

                  </div>
                );
              })
            )}

          </section>
        )}

      </main>

      {/* CART */}
      {isCartOpen && (
        <div className="cart-overlay" onClick={() => setIsCartOpen(false)}>

          <div className="cart-drawer" onClick={(e) => e.stopPropagation()}>

            <h2>Shopping Cart</h2>

            {cart?.items?.length === 0 ? (
              <p>Cart is empty</p>
            ) : (
              <>
                {cart.items.map(item => (
                  <div key={item.variantId} className="cart-item">
                    <p>{item.name}</p>
                    <p>Qty: {item.quantity}</p>

                    <p>€ {Number(item.price || 0).toFixed(2)}</p>

                    <button onClick={() => removeFromCart(item.variantId)}>
                      Remove
                    </button>
                  </div>
                ))}

                <hr />

                <h3>
                  Total: € {totalPrice.toFixed(2)}
                </h3>
              </>
            )}

            <button onClick={() => setIsCartOpen(false)}>
              Close
            </button>

          </div>
        </div>
      )}

      {/* FOOTER */}
      <footer className="customer-footer">
        <ul className="footer-links">
          {links.map(link => (
            <li key={link}>{link}</li>
          ))}
        </ul>
      </footer>

    </div>
  );
}

export default CustomerHome;