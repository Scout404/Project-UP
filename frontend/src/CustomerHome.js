import React from 'react';
import './CustomerHome.css';

function CustomerHome({ user, onLogout }) {
  const categories = ['home', 'clothes', 'accessoires', 'collections'];
  const links = ['contact', 'about us', 'support'];

  return (
    <div className="customer-home">
      <header className="customer-header">
        <nav className="customer-nav">
          <ul className="customer-menu">
            {categories.map((category) => (
              <li key={category}>{category}</li>
            ))}
          </ul>

          <div className="customer-actions">
            <button className="icon-btn" aria-label="Search">🔍</button>
            <button className="icon-btn" aria-label="Favorites">❤️</button>
            <button className="icon-btn" aria-label="Bag">🛍️</button>
            <button className="icon-btn" aria-label="Profile">👤</button>
            <button className="logout-btn" onClick={onLogout}>Logout</button>
          </div>
        </nav>
      </header>

      <main className="customer-main">
        <section className="hero-panel">
          <div className="hero-copy">
            <p className="eyebrow">New look</p>
            <h1>Discover new looks.</h1>
            <p className="hero-text">
              Explore latest collection.
            </p>
            <button className="primary-btn">Shop now</button>
          </div>
          <div className="hero-visual">
            <div className="hero-image">
              <span>image</span>
            </div>
          </div>
        </section>
      </main>

      <footer className="customer-footer">
        <ul className="footer-links">
          {links.map((link) => (
            <li key={link}>{link}</li>
          ))}
        </ul>
      </footer>
    </div>
  );
}

export default CustomerHome;
