import React from "react";
// import "../styles/home.css";

const Home: React.FC = () => {
  return (
    <main className="home">

      {/* HERO SECTION */}
      <section className="hero">
        <div className="container">
          <h1>Welcome to Your Webshop</h1>
          <p>Your homepage starts here.</p>
        </div>
      </section>

      {/* FEATURED PRODUCTS */}
      <section className="featured-products">
        <div className="container">
          <h2>Featured Products</h2>

          {/* Product grid will go here */}
          <div className="product-grid">

          </div>

        </div>
      </section>

      {/* CATEGORIES */}
      <section className="categories">
        <div className="container">
          <h2>Categories</h2>

          <div className="category-grid">

          </div>

        </div>
      </section>

    </main>
  );
};

export default Home;