import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { apiUrl } from "./api";

const CartContext = createContext();
const GUEST_CART_KEY = "guestCart";

function readGuestCart() {
  try {
    const storedCart = localStorage.getItem(GUEST_CART_KEY);
    return storedCart ? JSON.parse(storedCart) : { items: [] };
  } catch (err) {
    console.error("[CART DEBUG] Failed to parse guest cart:", err);
    localStorage.removeItem(GUEST_CART_KEY);
    return { items: [] };
  }
}

function saveGuestCart(cart) {
  localStorage.setItem(GUEST_CART_KEY, JSON.stringify(cart));
}

export function CartProvider({ children, user }) {
  const [cart, setCart] = useState({ items: [] });
  
  const rawUserId = !user?.isGuest && (user?.id ?? user?.customer_id);
  const parsedUserId = rawUserId ? Number(rawUserId) : null;
  const userId = Number.isFinite(parsedUserId) ? parsedUserId : null;
  const currentUserIdRef = useRef(userId);
  currentUserIdRef.current = userId;
  
  console.log("[CART DEBUG] User:", user);
  console.log("[CART DEBUG] Extracted userId:", userId);

  const fetchCart = useCallback(async (cartUserId) => {
    if (!cartUserId) {
      console.warn("[CART DEBUG] Cannot fetch cart - userId is null");
      return;
    }

    console.log(`[CART DEBUG] Fetching cart for userId: ${cartUserId}`);
    try {
      const res = await fetch(apiUrl(`/cart/${cartUserId}`));
      if (!res.ok) {
        console.error(`[CART DEBUG] Fetch failed with status ${res.status}`);
        return;
      }
      
      const data = await res.json();
      console.log("[CART DEBUG] Cart fetched:", data);
      if (currentUserIdRef.current === cartUserId) {
        setCart(data);
      }
    } catch (err) {
      console.error("[CART DEBUG] Cart fetch error:", err);
    }
  }, []);

  useEffect(() => {
    console.log("[CART DEBUG] useEffect: userId changed to", userId);
    setCart({ items: [] });

    if (userId) {
      fetchCart(userId);
    } else {
      setCart(readGuestCart());
    }
  }, [fetchCart, userId]);

  const addToCart = async (product) => {
    console.log("[CART DEBUG] addToCart called with:", product);
    console.log("[CART DEBUG] Current userId:", userId);
    
    const payload = {
      variantId: product.variantId,
      name: product.name,
      price: Number(product.price || 0),
      quantity: Number(product.quantity || 1)
    };

    if (!userId) {
      setCart((currentCart) => {
        const existingItem = currentCart.items.find(
          item => item.variantId === payload.variantId
        );

        const nextCart = existingItem
          ? {
              items: currentCart.items.map(item =>
                item.variantId === payload.variantId
                  ? { ...item, quantity: item.quantity + payload.quantity }
                  : item
              )
            }
          : {
              items: [...currentCart.items, payload]
            };

        saveGuestCart(nextCart);
        return nextCart;
      });

      return;
    }

    console.log(`[CART DEBUG] POST /cart/add/${userId}`, payload);

    try {
      const res = await fetch(apiUrl(`/cart/add/${userId}`), {
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
      
      await fetchCart(userId);
    } catch (err) {
      console.error("[CART DEBUG] addToCart error:", err);
    }
  };

  // clear cart after check out
  const clearCart = async () => {
    setCart({ items: [] });

    if (!userId) {
      localStorage.removeItem(GUEST_CART_KEY);
      return;
    }

    try {
      await fetch(apiUrl(`/cart/clear/${userId}`), {
        method: "DELETE"
      });
    } catch (err) {
      console.error(err);
    }
  };

  const removeFromCart = async (variantId) => {
    if (!userId) {
      setCart((currentCart) => {
        const nextCart = {
          items: currentCart.items.filter(item => item.variantId !== variantId)
        };

        saveGuestCart(nextCart);
        return nextCart;
      });

      return;
    }


    console.log(`[CART DEBUG] DELETE /cart/remove/${userId}/${variantId}`);

    try {
      const res = await fetch(apiUrl(`/cart/remove/${userId}/${variantId}`), {
        method: "DELETE"
      });

      if (!res.ok) {
        console.error(`[CART DEBUG] DELETE failed with status ${res.status}`);
        return;
      }

      await fetchCart(userId);
    } catch (err) {
      console.error("[CART DEBUG] removeFromCart error:", err);
    }
  };

  return (
    <CartContext.Provider value={{ cart, addToCart, removeFromCart, clearCart }}>
      {children}
    </CartContext.Provider>
  );
}

export const useCart = () => useContext(CartContext);
