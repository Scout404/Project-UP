import React, { useState } from 'react';
import './LoginModal.css';

function LoginModal({ isOpen, onClose, onLoginSuccess }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await fetch('http://localhost:5050/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username: username,
          password: password,
        }),
      });

      if (response.ok) {
        const data = await response.json();
        const userData = {
          id: data.id,
          username: data.username,
          role: data.role
        };
        onLoginSuccess(userData);
        setUsername('');
        setPassword('');
        onClose(); // Close modal after successful login
      } else if (response.status === 401) {
        setError('Invalid username or password');
      } else {
        setError('Login failed. Please try again.');
      }
    } catch (err) {
      setError('Error connecting to server. Make sure the backend is running.');
      console.error('Login error:', err);
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="login-modal-overlay" onClick={onClose}>
      <div className="login-modal" onClick={(e) => e.stopPropagation()}>
        
        <button className="modal-close" onClick={onClose}>✕</button>

        <h2>Login</h2>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="username">Username:</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password:</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              required
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <button type="submit" disabled={loading} className="submit-btn">
            {loading ? 'Logging in...' : 'Login'}
          </button>
        </form>

        <div className="test-credentials">
          <p><strong>Test Credentials:</strong></p>
          <p>Admin: <code>admin</code> / <code>admin123</code></p>
          <p>Customer: <code>testuser</code> / <code>test123</code></p>
        </div>
      </div>
    </div>
  );
}

export default LoginModal;