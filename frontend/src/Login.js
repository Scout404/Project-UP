import React, { useState } from 'react';
import './Login.css';

function Login({ onLoginSuccess, onGuestContinue }) {
  const [mode, setMode] = useState('login');
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const endpoint = mode === 'login' ? 'login' : 'register';
      const response = await fetch(`http://localhost:5050/${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(
          mode === 'login'
            ? {
                username,
                password,
              }
            : {
                username,
                email,
                password,
              }
        ),
      });

      if (response.ok) {
        const data = await response.json();
        const userData = {
          id: data.id,
          username: data.username, 
          role: data.role 
        };
        localStorage.setItem('user', JSON.stringify(userData));
        onLoginSuccess(userData);
      } else if (response.status === 401) {
        setError('Invalid username or password');
      } else if (response.status === 409) {
        setError('Username or email already exists');
      } else {
        setError(mode === 'login' ? 'Login failed. Please try again.' : 'Registration failed. Please try again.');
      }
    } catch (err) {
      setError('Error connecting to server. Make sure the backend is running.');
      console.error(`${mode} error:`, err);
    } finally {
      setLoading(false);
    }
  };

  const switchMode = (nextMode) => {
    setMode(nextMode);
    setError('');
  };

  return (
    <div className="login-container">
      <div className="login-box">
        <h1>Welcome</h1>

        <div className="auth-tabs" aria-label="Account options">
          <button
            type="button"
            className={mode === 'login' ? 'active' : ''}
            onClick={() => switchMode('login')}
          >
            Login
          </button>
          <button
            type="button"
            className={mode === 'register' ? 'active' : ''}
            onClick={() => switchMode('register')}
          >
            Account maken
          </button>
        </div>
        
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

          {mode === 'register' && (
            <div className="form-group">
              <label htmlFor="email">Email:</label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Enter your email"
                required
              />
            </div>
          )}

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

          <button type="submit" disabled={loading} className="login-btn">
            {loading
              ? mode === 'login' ? 'Logging in...' : 'Creating account...'
              : mode === 'login' ? 'Login' : 'Account maken'}
          </button>
        </form>

        <div className="divider">
          <span>or</span>
        </div>

        <button onClick={onGuestContinue} className="guest-btn">
          Continue as Guest
        </button>

        {mode === 'login' && <div className="test-credentials">
          <p><strong>Test Credentials:</strong></p>
          <p>Admin - Username: <code>admin</code></p>
          <p>Admin - Password: <code>admin123</code></p>
          <p>Or</p>
          <p>Customer - Username: <code>testuser</code></p>
          <p>Customer - Password: <code>test123</code></p>
        </div>}
      </div>
    </div>
  );
}

export default Login;
