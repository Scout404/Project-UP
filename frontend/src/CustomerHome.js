import React from 'react';
import './CustomerHome.css';

function CustomerHome({ user, onLogout }) {
  return (
    <div className="customer-home">
      <nav className="customer-navbar">
        <div className="navbar-brand">
          <h1>Welcome to Our Webshop</h1>
        </div>
        <div className="navbar-user">
          <span>Hello, {user.username}!</span>
          <button onClick={onLogout} className="logout-btn">Logout</button>
        </div>
      </nav>
    </div>
  );
}

export default CustomerHome;
