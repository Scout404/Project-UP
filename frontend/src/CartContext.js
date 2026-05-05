import { createContext, useContext, useEffect, useState } from "react";

const CartContext = createContext();

export function CartProvider({ children, user }) {
  const [cart, setCart] = useState({ items: [] });
  
  const userId = user?.id ? Number(user.id) : (user?.customer_id ? Number(user.customer_id) : null);
  
  console.log("[CART DEBUG] User:", user);
  console.log("[CART DEBUG] Extracted userId:", userId);

  const fetchCart = async () => {
    if (!userId) {
      console.warn("[CART DEBUG] Cannot fetch cart - userId is null");
      return;
    }

    console.log(`[CART DEBUG] Fetching cart for userId: ${userId}`);
    try {
      const res = await fetch(`http://localhost:5050/cart/${userId}`);
      if (!res.ok) {
        console.error(`[CART DEBUG] Fetch failed with status ${res.status}`);
        return;
      }
      
      const data = await res.json();
      console.log("[CART DEBUG] Cart fetched:", data);
      setCart(data);
    } catch (err) {
      console.error("[CART DEBUG] Cart fetch error:", err);
    }
  };

  useEffect(() => {
    console.log("[CART DEBUG] useEffect: userId changed to", userId);
    fetchCart();
  }, [userId]);

  const addToCart = async (product) => {
    console.log("[CART DEBUG] addToCart called with:", product);
    console.log("[CART DEBUG] Current userId:", userId);
    
    if (!userId) {
      console.error("[CART DEBUG] Cannot add to cart - userId is null!");
      return;
    }

    const payload = {
      variantId: product.variantId,
      name: product.name,
      price: Number(product.price || 0),
      quantity: 1
    };

    console.log(`[CART DEBUG] POST /cart/add/${userId}`, payload);

    try {
      const res = await fetch(`http://localhost:5050/cart/add/${userId}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      if (!res.ok) {
        console.error(`[CART DEBUG] POST failed with status ${res.status}`);
        return;
      }

      const data = await res.json();
      console.log("[CART DEBUG] POST response:", data);
      
      await fetchCart();
    } catch (err) {
      console.error("[CART DEBUG] addToCart error:", err);
    }
  };

  const removeFromCart = async (variantId) => {
    if (!userId) return;

    console.log(`[CART DEBUG] DELETE /cart/remove/${userId}/${variantId}`);

    try {
      const res = await fetch(`http://localhost:5050/cart/remove/${userId}/${variantId}`, {
        method: "DELETE"
      });

      if (!res.ok) {
        console.error(`[CART DEBUG] DELETE failed with status ${res.status}`);
        return;
      }

      await fetchCart();
    } catch (err) {
      console.error("[CART DEBUG] removeFromCart error:", err);
    }
  };

  return (
    <CartContext.Provider value={{ cart, addToCart, removeFromCart }}>
      {children}
    </CartContext.Provider>
  );
}

export const useCart = () => useContext(CartContext);