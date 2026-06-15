import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CartProvider, useCart } from "../CartContext";

function CartProbe() {
  const { cart, addToCart, removeFromCart, clearCart } = useCart();

  return (
    <div>
      <p>Items: {cart.items.length}</p>
      <p>Quantity: {cart.items[0]?.quantity || 0}</p>
      <button
        type="button"
        onClick={() =>
          addToCart({
            variantId: 42,
            name: "Chino Pants",
            price: 80,
            quantity: 1
          })
        }
      >
        Add chino
      </button>
      <button type="button" onClick={() => removeFromCart(42)}>
        Remove chino
      </button>
      <button type="button" onClick={clearCart}>
        Clear cart
      </button>
    </div>
  );
}

describe("guest cart", () => {
  beforeEach(() => {
    localStorage.clear();
    jest.spyOn(console, "log").mockImplementation(() => {});
    jest.spyOn(console, "warn").mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  test("adds duplicate products as quantity increases and persists them in localStorage", async () => {
    render(
      <CartProvider user={null}>
        <CartProbe />
      </CartProvider>
    );

    await userEvent.click(screen.getByRole("button", { name: /add chino/i }));
    await userEvent.click(screen.getByRole("button", { name: /add chino/i }));

    await waitFor(() => {
      expect(screen.getByText("Items: 1")).toBeInTheDocument();
      expect(screen.getByText("Quantity: 2")).toBeInTheDocument();
    });

    expect(JSON.parse(localStorage.getItem("guestCart"))).toEqual({
      items: [
        {
          variantId: 42,
          name: "Chino Pants",
          price: 80,
          quantity: 2
        }
      ]
    });
  });

  test("removes and clears guest cart data", async () => {
    localStorage.setItem(
      "guestCart",
      JSON.stringify({
        items: [{ variantId: 42, name: "Chino Pants", price: 80, quantity: 1 }]
      })
    );

    render(
      <CartProvider user={null}>
        <CartProbe />
      </CartProvider>
    );

    await waitFor(() => {
      expect(screen.getByText("Items: 1")).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole("button", { name: /remove chino/i }));

    await waitFor(() => {
      expect(screen.getByText("Items: 0")).toBeInTheDocument();
    });
    expect(JSON.parse(localStorage.getItem("guestCart"))).toEqual({ items: [] });

    await userEvent.click(screen.getByRole("button", { name: /clear cart/i }));
    expect(localStorage.getItem("guestCart")).toBeNull();
  });
});
