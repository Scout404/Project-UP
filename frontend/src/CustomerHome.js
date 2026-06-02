import React, { useState, useEffect, useRef } from 'react';
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
  const [searchQuery, setSearchQuery] = useState("");
  const searchInputRef = useRef(null);

  const [street, setStreet] = useState("");
  const [city, setCity] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [country, setCountry] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("Card");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetch("http://localhost:5050/products")
      .then(res => res.json())
      .then(setProducts)
      .catch(err => console.error("Failed to fetch products:", err));
  }, []);

  const normalizedSearch = searchQuery.trim().toLowerCase();
  const isSearching = normalizedSearch.length > 0;

  const filteredProducts = products.filter((p) => {
    if (!p.isActive) {
      return false;
    }

    const categoryName = p.categoryName?.toLowerCase() || "";
    const matchesCategory = activePage === "home" || categoryName === activePage;
    const searchableText = [
      p.name,
      p.brand,
      p.description,
      p.categoryName
    ].join(" ").toLowerCase();

    return matchesCategory && (!isSearching || searchableText.includes(normalizedSearch));
  });

  const showProducts = (activePage !== "home" && activePage !== "checkout") || isSearching; //show prod

  const totalPrice =
    cart?.items?.reduce(
      (sum, item) => sum + (Number(item.price) || 0) * item.quantity,
      0
    ) || 0;

  const handleLoginSuccess = (userData) => {
    setIsLoginModalOpen(false);
    onLoginSuccess(userData);
  };


  const handleCheckout = async () => {
    try {
      setLoading(true);

      // delay
      await new Promise(res => setTimeout(res, 2000));

      const userId = user?.id || localStorage.getItem("userId");
      if (!userId) {
      console.error("NO USER ID FOUND");
      alert("You must be logged in");
      return;
      }

      console.log("USER:", user);
      console.log("LOCAL USERID:", localStorage.getItem("userId"));

      const res = await fetch(`http://localhost:5050/checkout/${userId}`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          street,
          city,
          postalCode,
          country,
          paymentMethod
        })
      });


      const text = await res.text();
      let data;

      try {
        data = JSON.parse(text);
      } catch {
        data = text;
      }

      console.log("CHECKOUT RESPONSE:", data);

      setActivePage("Order was a success");

    } catch (err) {
      console.error(err);
      alert("Checkout failed");
    } finally {
      setLoading(false);
    }
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
        {showProducts && (
          <section className="products-grid">

            <h2>
              {isSearching
                ? `Search results for "${searchQuery.trim()}"`
                : activePage.charAt(0).toUpperCase() + activePage.slice(1)}
            </h2>

            {filteredProducts.length === 0 ? (
              <p>see you soon!</p>
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

        {/* CHECKOUT */}
        {activePage === "checkout" && (
          <section className="checkout-page">
            <h2>Checkout</h2>

            {loading ? (
              <p>Processing payment...</p>
            ) : (
              <>
              <label>Street</label>
              <input
                value={street}
                onChange={(e) => setStreet(e.target.value)}
              />

              <label>City</label>
              <input
                value={city}
                onChange={(e) => setCity(e.target.value)}
              />

              <label>Postal Code</label>
              <input
                value={postalCode}
                onChange={(e) => setPostalCode(e.target.value)}
              />

              <label>Country</label>
              <input
                value={country}
                onChange={(e) => setCountry(e.target.value)}
              />

                <select
                  value={paymentMethod}
                  onChange={(e) => setPaymentMethod(e.target.value)}
                >
                  <option value="Card">Card</option>
                  <option value="PayPal">PayPal</option>
                  <option value="Klarna">Klarna</option>
                </select>

                <button className="primary-btn" onClick={handleCheckout}>
                  Place Order
                </button>
              </>
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
            <div className = "cart-ActionsAndCheckout">
              <button onClick={() => setIsCartOpen(false)}>
                Continue Shopping
              </button>

              <button
                className="cartbutton"
                onClick={() => {
                  setIsCartOpen(false);
                  setActivePage("checkout");
                }}
              >
                  Checkout
              </button>

          </div>
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
