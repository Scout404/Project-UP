import React, { useState, useEffect } from 'react';
import './App.css';
import Login from './Login';
import AdminDashboard from './AdminDashboard';
import CustomerHome from './CustomerHome';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [user, setUser] = useState(null);

  useEffect(() => {
    // Check if user is already logged in
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
      setIsLoggedIn(true);
    }
  }, []);

  const handleLoginSuccess = (userData) => {
    setUser(userData);
    setIsLoggedIn(true);
  };

  const handleLogout = () => {
    localStorage.removeItem('user');
    setUser(null);
    setIsLoggedIn(false);
  };

  if (!isLoggedIn) {
    return <Login onLoginSuccess={handleLoginSuccess} />;
  }

  // Route based on user role
  if (user.role === 'Admin') {
    return <AdminDashboard user={user} onLogout={handleLogout} />;
  } else if (user.role === 'Customer') {
    return <CustomerHome user={user} onLogout={handleLogout} />;
  }

  // Fallback (shouldn't happen)
  return (
    <div className="App">
      <header className="App-header">
        <div className="navbar">
          <h1>Welcome, {user.username}!</h1>
          <button onClick={handleLogout} className="logout-btn">
            Logout
          </button>
        </div>
      </header>
      <main>
        <p>Unknown user role.</p>
      </main>
    </div>
  );
}

export default App;
