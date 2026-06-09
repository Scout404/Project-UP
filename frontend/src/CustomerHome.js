import React, { useState, useEffect, useRef } from 'react';
import './CustomerHome.css';
import LoginModal from './LoginModal';
import ProductDetailModal from './ProductDetailModal';
import { useCart } from './CartContext';
import { apiUrl } from './api';

function resolveImageUrl(product) {
  const imageUrl = product?.imageUrl || product?.image || product?.productImage || product?.pictureUrl;

  if (!imageUrl) {
    return '';
  }

  if (/^(https?:|data:|blob:)/i.test(imageUrl)) {
    return imageUrl;
  }

  return apiUrl(imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`);
}

function CustomerHome({ user, onLogout, onLoginSuccess }) {
  const categories = ['home', 'clothes', 'accessoires', 'collections'];
  const links = ['contact', 'about us', 'support'];
  const isLoggedIn = Boolean(user && !user.isGuest);

  const { cart, addToCart, removeFromCart } = useCart();

  const [isCartOpen, setIsCartOpen] = useState(false);
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [products, setProducts] = useState([]);
  const [activePage, setActivePage] = useState("home");
  const [cartMessage, setCartMessage] = useState("");
  const cartMessageTimerRef = useRef(null);
  const [searchQuery, setSearchQuery] = useState("");
  const searchInputRef = useRef(null);

  useEffect(() => {
    fetch(apiUrl("/products"))
      .then(res => res.json())
      .then(setProducts)
      .catch(err => console.error("Failed to fetch products:", err));
  }, []);

  useEffect(() => {
    return () => window.clearTimeout(cartMessageTimerRef.current);
  }, []);

  const normalizedSearchQuery = searchQuery.trim().toLowerCase();
  const isSearching = normalizedSearchQuery.length > 0;
  const showProducts = activePage !== "home" || isSearching;

  const filteredProducts = products.filter((product) => {
    if (!product.isActive) {
      return false;
    }

    if (isSearching) {
      const searchableText = [
        product.name,
        product.brand,
        product.categoryName
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return searchableText.includes(normalizedSearchQuery);
    }

    return product.categoryName?.toLowerCase() === activePage;
  });

  const totalPrice =
    cart?.items?.reduce(
      (sum, item) => sum + (Number(item.price) || 0) * item.quantity,
      0
    ) || 0;

  const handleLoginSuccess = (userData) => {
    setIsLoginModalOpen(false);
    onLoginSuccess(userData);
  };

  const handleAddToCart = (product) => {
    addToCart(product);
    setCartMessage(`${product.name} added to cart`);

    window.clearTimeout(cartMessageTimerRef.current);
    cartMessageTimerRef.current = window.setTimeout(() => {
      setCartMessage("");
    }, 1800);
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
                onClick={() => {
                  setActivePage(category);
                  if (category === "home") {
                    setSearchQuery("");
                  }
                }}
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
            <form className="search-form" onSubmit={(e) => e.preventDefault()}>
              <button
                type="button"
                className="icon-btn"
                title="Search products"
                onClick={() => searchInputRef.current?.focus()}
              >
                🔍
              </button>
              <input
                ref={searchInputRef}
                type="search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search products"
                aria-label="Search products"
                className="search-input"
              />
            </form>
            <button className="icon-btn">❤️</button>

            <button
              className="icon-btn"
              onClick={() => setIsCartOpen(true)}
            >
              🛒
            </button>

            {/* Account button - opens login modal if guest, shows user menu if logged in */}
            {!isLoggedIn ? (
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
            {isLoggedIn && (
              <button className="logout-btn" onClick={onLogout}>
                Logout
              </button>
            )}

            {/* Login button - only visible if guest */}
            {!isLoggedIn && (
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

      <ProductDetailModal
        product={selectedProduct}
        isOpen={Boolean(selectedProduct)}
        onClose={() => setSelectedProduct(null)}
        user={user}
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
        {showProducts && (
          <section className="products-grid">

            <h2>
              {isSearching
                ? `Search results for "${searchQuery.trim()}"`
                : activePage.charAt(0).toUpperCase() + activePage.slice(1)}
            </h2>

            {filteredProducts.length === 0 ? (
              <p>No items found.</p>
            ) : (
              filteredProducts.map(p => {
                const id = p.productId; 
                const price = p.basePrice || 0;

                return (
                  <div
                    key={id}
                    className="product-card"
                    role="button"
                    tabIndex="0"
                    onClick={() => setSelectedProduct(p)}
                    onKeyDown={(event) => {
                      if (event.target === event.currentTarget && (event.key === 'Enter' || event.key === ' ')) {
                        event.preventDefault();
                        setSelectedProduct(p);
                      }
                    }}
                  >
                    <div className="product-card-media">
                      {resolveImageUrl(p) ? (
                        <img src={resolveImageUrl(p)} alt={p.name} />
                      ) : (
                        <span>Coming Soon</span>
                      )}
                    </div>
                    <h3>{p.name}</h3>
                    <p className="brand-tag">{p.brand}</p>
                    <p className="price-tag">€ {Number(price).toFixed(2)}</p>

                    <button
                      onClick={(event) => {
                        event.stopPropagation();
                        handleAddToCart({
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

      {cartMessage && (
        <p className="cart-message" role="status">
          {cartMessage}
        </p>
      )}

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
