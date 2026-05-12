import React, { useState, useEffect } from 'react';
import './App.css';
import CustomerHome from './CustomerHome';
import AdminPanel from './AdminPage';
import { CartProvider } from './CartContext';

function App() {
  const [user, setUser] = useState(null);
  const [isLoadingUser, setIsLoadingUser] = useState(true);

  useEffect(() => {
    // If user is already logged in
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        const userData = JSON.parse(storedUser);
        console.log('[APP DEBUG] Loaded user from localStorage:', userData);
        setUser(userData);
      } catch (err) {
        console.error('Failed to parse stored user:', err);
        localStorage.removeItem('user');
      }
    }
    setIsLoadingUser(false);
  }, []);

  const handleLoginSuccess = (userData) => {
    console.log('[APP DEBUG] User logged in:', userData);
    setUser(userData);
    localStorage.setItem('user', JSON.stringify(userData));
  };

  const handleLogout = () => {
    console.log('[APP DEBUG] User logged out');
    setUser(null);
    localStorage.removeItem('user');
  };

  if (isLoadingUser) {
    return <div>Loading...</div>;
  }

  // Admin view (only if logged in AND has Admin role)
  if (user?.role === 'Admin') {
    console.log('[APP DEBUG] Rendering AdminPanel');
    return <AdminPanel user={user} onLogout={handleLogout} />;
  }

  // Customer/Guest view (logged in customer OR guest browsing)
  console.log('[APP DEBUG] Rendering CustomerHome (user:', user ? 'logged-in' : 'guest', ')');
  return (
    <CartProvider user={user}>
      <CustomerHome user={user} onLogout={handleLogout} onLoginSuccess={handleLoginSuccess} />
    </CartProvider>
  );
}

export default App;