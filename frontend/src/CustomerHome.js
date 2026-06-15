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

  const { cart, addToCart, removeFromCart, clearCart} = useCart();

  const [isCartOpen, setIsCartOpen] = useState(false);
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [products, setProducts] = useState([]);
  const [activePage, setActivePage] = useState("home");
  const [cartMessage, setCartMessage] = useState("");
  const cartMessageTimerRef = useRef(null);
  const [searchQuery, setSearchQuery] = useState("");
  const searchInputRef = useRef(null);

  const [showCheckout, setShowCheckout] = useState(false);
  const [loading, setLoading] = useState(false);


  // loading bar payment
  const [progress, setProgress] = useState(0);

  // checkout info form
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [street, setStreet] = useState("");
  const [city, setCity] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [country, setCountry] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("Apple Pay");

  const [wishlist, setWishlist] = useState([]);
  const [showWishlist, setShowWishlist] = useState(false);

  const [page, setPage] = useState("home");


  useEffect(() => {
    fetch(apiUrl("/products"))
      .then(res => res.json())
      .then(setProducts)
      .catch(err => console.error("Failed to fetch products:", err));
  }, []);

  useEffect(() => {
    return () => window.clearTimeout(cartMessageTimerRef.current);
  }, []);

  // wishlist after youre logged in
  useEffect(() => {
    if (!user?.id) return;

    fetch(apiUrl(`/wishlist/${user.id}`))
      .then(res => res.json())
      .then(setWishlist)
      .catch(err => console.error(err));
  }, [user]);

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

  const openProductDetails = (product) => {
    setSelectedProduct(product);
  };

  // checkout logic
  const handleCheckout = async () => {
    if (email && !email.includes("@")) 
    {
      alert("Invalid email");
      return;
    }

    if (!cart?.items || cart.items.length === 0) 
    {
      alert("Cart is empty");
      return;
    }

    setLoading(true);
    setProgress(0);

    // 5 second payment screen
    let percent = 0;

    const interval = setInterval(() => 
    {
      percent += 2;
      setProgress(percent);

      if (percent >= 100) 
      {
        clearInterval(interval);
      }
    }, 100);

    try {
      await new Promise((res) => setTimeout(res, 5000));

      const res = await fetch(apiUrl("/checkout"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          FirstName: firstName,
          LastName: lastName,
          Email: email,
          Street: street,
          City: city,
          PostalCode: postalCode,
          Country: country,
          PaymentMethod: paymentMethod,
          Items: cart.items.map(item => ({
          VariantId: item.variantId,
          Name: item.name,
          Price: item.price,
          Quantity: item.quantity
        }))
        })
      });

      if (!res.ok)
      {
          const message = await res.text();
          throw new Error(message);
      }

      const data = await res.json();
      alert(data.message || "Order placed. The receipt was written to backend/OrderReceipts/orders.txt");

      //clear
      clearCart();

      // and clear user info after checking out
      setFirstName("");
      setLastName("");
      setEmail("");
      setStreet("");
      setCity("");
      setPostalCode("");
      setCountry("");
      setPaymentMethod("");

      setShowCheckout(false);

    } catch (err) {
      alert(err.message);
    } finally {
      setLoading(false);
      setProgress(0);
    }
  };


  // Toggle wishlist logic 
  const toggleWishlist = async (productId) => {
    if (!user?.id) {
      alert("Login required");
      return;
    }

    const url = apiUrl(`/wishlist/${user.id}/${productId}`);
    const isInWishlist = wishlist.includes(productId);

    await fetch(url, {
      method: isInWishlist ? "DELETE" : "POST"
    });

    setWishlist(prev => {
      if (prev.includes(productId)) {
        // remove 
        return prev.filter(id => id !== productId);
      }
      // add 
      return prev.concat(productId);
    });
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
                  setPage("home");
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
            {/* <button className="icon-btn">❤️</button> */}
            <button
              className="icon-btn"
              onClick={() => setShowWishlist(true)}
            >
              ❤️
            </button>
            

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
          <section className="hero-panel home-hero" aria-label="Rene Clothes homepage">
            <div className="hero-copy">
              <h1>Rene Clothes</h1>
              <p className="hero-text">TIMELESS STYLE. MODERN YOU.</p>
            </div>
          </section>
        )}

        {/* CONTACT PAGE */}
        {page === "contact" && (
          <section className="hero-panel" style={{ padding: "2rem" }}>
            <div className="hero-copy">
              <h1>Contact</h1>
              <p>You can contact us on:</p>
              <p>Email: info@newlook.com</p>

              <button className="primary-btn" onClick={() => setPage("home")}>
                Back
              </button>
            </div>
          </section>
        )}
        {/* About us PAGE */}
        {page === "about us" && (
          <section className="hero-panel" style={{ padding: "2rem" }}>
            <div className="hero-copy">
              <h1>About us</h1>
              <p>Newlook is a global fashion brand that belives in sustainability and style.</p>
              <p>We believe that good quality clothes can be affordable too!</p>

              <button className="primary-btn" onClick={() => setPage("home")}>
                Back
              </button>
            </div>
          </section>
        )}
        {/* About us PAGE */}
        {page === "support" && (
          <section className="hero-panel" style={{ padding: "2rem" }}>
            <div className="hero-copy">
              <h1>Support</h1>
              <h2>FAQ</h2> 
              <h4>Where is my order?</h4>
                <p>Orders take between 3-5 days to arrive, using the track and trace code you can follow your order.</p>
              <h4>Item did not arrive</h4>
                <p>contact our customer-service.</p>

              <h4>How many days do i have to return an item?</h4>
                <p>You have 14 days to return an item.</p>

              <button className="primary-btn" onClick={() => setPage("home")}>
                Back
              </button>
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
                    onClick={() => openProductDetails(p)}
                    onKeyDown={(event) => {
                      if (event.target === event.currentTarget && (event.key === 'Enter' || event.key === ' ')) {
                        event.preventDefault();
                        openProductDetails(p);
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
                      {/* HEART BUTTON */}
                    <button
                      className="heart-btn"
                      onClick={(e) => {
                        e.stopPropagation();
                        toggleWishlist(id);
                      }}
                    >
                      {wishlist.includes(id) ? "❤️" : "🤍"}
                    </button>

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

            <h2>Cart</h2>

            {cart.items.map(item => (
              <div
                key={item.variantId}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  marginBottom: "12px",
                  gap: "10px"
                }}
              >
                <span>{item.name} x {item.quantity}</span>

                <button
                  className="primary-btn"
                  onClick={() => removeFromCart(item.variantId)}
                >
                  Remove
                </button>
              </div>
            ))}


            <hr />

            {/* total price */}
            <h3>
              Total: € {totalPrice.toFixed(2)}
            </h3>

            <hr />

            {/* checkout button */}
            <button
              className="primary-btn"
              onClick={() => {
                if (!cart?.items || cart.items.length === 0) {
                  alert("Your cart is empty");
                  return;
                }
                setShowCheckout(true);
              }}
            >
              Checkout
            </button>

            <button
              className="primary-btn"
              onClick={() => setIsCartOpen(false)}
            >
              Close
            </button>

          </div>
        </div>
      )}
      {/* WISHLIST overlay */}
      {showWishlist && (
        <div className="cart-overlay" onClick={() => setShowWishlist(false)}>
          <div className="cart-drawer" onClick={(e) => e.stopPropagation()}>

            <h2>Wishlist</h2>

            {/* Loop through all products */}
            {products.map(product => {

              // Check if this product is in the wishlist
              const isInWishlist = wishlist.includes(product.productId);
              // Only show it if it's in the wishlist
              if (!isInWishlist) 
              {
                return null; 
              }
              return (
                <div
                  key={product.productId}
                  role="button"
                  tabIndex="0"
                  onClick={() => {
                    setShowWishlist(false);
                    openProductDetails(product);
                  }}
                  onKeyDown={(event) => {
                    if (event.target === event.currentTarget && (event.key === 'Enter' || event.key === ' ')) {
                      event.preventDefault();
                      setShowWishlist(false);
                      openProductDetails(product);
                    }
                  }}
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    cursor: "pointer"
                  }}
                >
                  <span>{product.name}</span>

                  <button
                  className="primary-btn"
                  onClick={(event) => {
                    event.stopPropagation();
                    toggleWishlist(product.productId);
                  }}
                >
                  Remove
                </button>
                </div>
              );
            })}

            {wishlist.length === 0 && (
              <p>No items in wishlist</p>
            )}

            <button
              className="primary-btn"
              onClick={() => setShowWishlist(false)}
            >
              Close
            </button>


          </div>
        </div>
      )}


      {/* checkout */}
      {showCheckout && (
        <div className="cart-overlay" onClick={() => setShowCheckout(false)}>
          <div className="cart-drawer" onClick={(e) => e.stopPropagation()}>

            <h2>Checkout</h2>

            <input placeholder="First Name" value={firstName} onChange={e => setFirstName(e.target.value)} />
            <input placeholder="Last Name" value={lastName} onChange={e => setLastName(e.target.value)} />
            <input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
            <input placeholder="Street" value={street} onChange={e => setStreet(e.target.value)} />
            <input placeholder="City" value={city} onChange={e => setCity(e.target.value)} />
            <input placeholder="Postal Code" value={postalCode} onChange={e => setPostalCode(e.target.value)} />
            <input placeholder="Country" value={country} onChange={e => setCountry(e.target.value)} />

            {/* card options */}
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
            >
              <option value="APPLE_PAY">Apple Pay</option>
              <option value="PAYPAL">PayPal</option>
              <option value="ING">ING</option>
              <option value="RABOBANK">Rabobank</option>
              <option value="ABN_AMRO">ABN AMRO</option>
            </select>

            {/* button */}
            <button
              className="primary-btn"
              disabled={loading}
              onClick={handleCheckout}
            >
              {loading ? "Processing payment..." : "Place Order"}
            </button>

            <button onClick={() => setShowCheckout(false)}>
              Cancel
            </button>

            {/* loading bar screen */}
            {loading && (
              <div style={{
                width: "100%",
                background: "#eee",
                height: "10px",
                marginTop: "10px",
                borderRadius: "6px"
              }}>
                <div
                  style={{
                    width: `${progress}%`,
                    height: "100%",
                    background: "black",
                    transition: "width 0.1s"
                  }}
                />
              </div>
            )}

          </div>
        </div>
      )}

      <footer className="customer-footer">
        <ul className="footer-links">
          {links.map(link => (
            <li
              key={link}
              onClick={() => setPage(link)}  
              style={{ cursor: "pointer" }}
            >
              {link}
            </li>
          ))}
        </ul>
      </footer>


    </div>
    
  );
}

export default CustomerHome;

