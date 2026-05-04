import React, { useState, useEffect } from 'react';
import './App.css';
import Login from './Login';
import AdminPage from './AdminPage';
import CustomerHome from './CustomerHome';
import { CartProvider } from "./CartContext";

function App() {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

useEffect(() => {
  try {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      const parsed = JSON.parse(savedUser);
      console.log("[APP DEBUG] Loaded user from localStorage:", parsed);
      setUser(parsed);
    }
  } catch (err) {
    console.error("Error parsing user:", err);
    localStorage.removeItem('user');
  }
  setLoading(false);
}, []);

const handleLoginSuccess = (userData) => {
  console.log("[APP DEBUG] userData:", userData);
  console.log("[APP DEBUG] userData.id:", userData?.id);
  console.log("[APP DEBUG] userData.customer_id:", userData?.customer_id);
  localStorage.setItem('user', JSON.stringify(userData));
  setUser(userData);
};

const handleLogout = () => {
    localStorage.removeItem('user');
    setUser(null);
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!user) {
    return <Login onLoginSuccess={handleLoginSuccess} />;
  }

  if (user.role === 'Customer') {
    console.log("[APP DEBUG] About to render CartProvider with user:", user);
    return (
      <CartProvider user={user}>
        <CustomerHome
          user={user}
          onLogout={handleLogout}
        />
      </CartProvider>
    );
  }
  
  if (user.role === 'Admin') {
    return (
      <AdminPage
        user={user}
        onLogout={handleLogout}
      />
    );
  }

  if (user.role === 'Customer') {
    return (
      <CartProvider user={user}>
        <CustomerHome
          user={user}
          onLogout={handleLogout}
        />
      </CartProvider>
    );
  }

  return (
    <div className="App">
      <h1>Unknown role: {user.role}</h1>
      <button onClick={handleLogout}>Logout</button>
    </div>
  );
}

export default App;