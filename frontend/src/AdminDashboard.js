import React from 'react';
import './AdminDashboard.css';

function AdminDashboard({ user, onLogout }) {
  return (
    <div className="admin-dashboard">
      <nav className="admin-navbar">
        <div className="navbar-brand">
          <h1>Admin Dashboard</h1>
        </div>
        <div className="navbar-user">
          <span>Welcome, {user.username}</span>
          <button onClick={onLogout} className="logout-btn">Logout</button>
        </div>
      </nav>
    </div>
  );
}

export default AdminDashboard;
