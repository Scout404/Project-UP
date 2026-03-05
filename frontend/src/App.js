import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Home from './pages/public/Home.tsx';
import "./App.css";

function App() {
  return (
    <Router>

      <Routes>

        <Route path="/" element={<Home />} />

      </Routes>

    </Router>
  );
}

export default App;