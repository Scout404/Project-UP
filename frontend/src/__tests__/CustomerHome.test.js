import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CustomerHome from "../CustomerHome";
import { CartProvider } from "../CartContext";

const products = [
  {
    productId: 101,
    name: "Slim Jeans",
    brand: "Denim Co",
    categoryName: "clothes",
    basePrice: 80,
    isActive: true
  },
  {
    productId: 102,
    name: "Oxford Shirt",
    brand: "Newlook",
    categoryName: "clothes",
    basePrice: 45,
    isActive: true
  },
  {
    productId: 103,
    name: "Winter Coat",
    brand: "Premium",
    categoryName: "clothes",
    basePrice: 199.99,
    isActive: false
  }
];

function jsonResponse(data, ok = true) {
  return Promise.resolve({
    ok,
    json: () => Promise.resolve(data),
    text: () => Promise.resolve(typeof data === "string" ? data : JSON.stringify(data))
  });
}

function renderCustomerHome(user = null) {
  return render(
    <CartProvider user={user}>
      <CustomerHome user={user} onLogout={jest.fn()} onLoginSuccess={jest.fn()} />
    </CartProvider>
  );
}

describe("customer shop high-risk flows", () => {
  beforeEach(() => {
    localStorage.clear();
    jest.spyOn(console, "log").mockImplementation(() => {});
    jest.spyOn(console, "warn").mockImplementation(() => {});
    jest.spyOn(console, "error").mockImplementation(() => {});
    jest.spyOn(window, "alert").mockImplementation(() => {});
    global.fetch = jest.fn((url, options = {}) => {
      const requestUrl = String(url);

      if (requestUrl.endsWith("/products")) {
        return jsonResponse(products);
      }

      if (requestUrl.endsWith("/checkout") && options.method === "POST") {
        return jsonResponse({ message: "Order placed" });
      }

      return jsonResponse([]);
    });
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  test("partial search returns active matching products and hides inactive products", async () => {
    renderCustomerHome();

    await userEvent.type(screen.getByLabelText(/search products/i), "jea");

    expect(await screen.findByRole("heading", { name: "Slim Jeans" })).toBeInTheDocument();
    expect(screen.getByText('Search results for "jea"')).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Oxford Shirt" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Winter Coat" })).not.toBeInTheDocument();
  });

  test("guest checkout posts the selected cart items and clears the cart after payment", async () => {
    jest.useFakeTimers();
    renderCustomerHome();

    await userEvent.type(screen.getByLabelText(/search products/i), "jea");
    await screen.findByRole("heading", { name: "Slim Jeans" });
    await userEvent.click(screen.getByText("Add to cart"));

    await waitFor(() => {
      expect(JSON.parse(localStorage.getItem("guestCart"))?.items).toHaveLength(1);
    });

    const actionButtons = document.querySelectorAll(".customer-actions .icon-btn");
    await userEvent.click(actionButtons[2]);

    const cartDrawer = screen.getByText("Cart").closest(".cart-drawer");
    expect(within(cartDrawer).getByText("Slim Jeans x 1")).toBeInTheDocument();
    expect(within(cartDrawer).getByText(/80\.00/)).toBeInTheDocument();

    await userEvent.click(within(cartDrawer).getByRole("button", { name: /checkout/i }));

    await userEvent.type(screen.getByPlaceholderText(/first name/i), "Mark");
    await userEvent.type(screen.getByPlaceholderText(/last name/i), "Tester");
    await userEvent.type(screen.getByPlaceholderText(/email/i), "mark.tester@example.com");
    await userEvent.type(screen.getByPlaceholderText(/street/i), "Coolsingel 1");
    await userEvent.type(screen.getByPlaceholderText(/city/i), "Rotterdam");
    await userEvent.type(screen.getByPlaceholderText(/postal code/i), "3012 AA");
    await userEvent.type(screen.getByPlaceholderText(/country/i), "Netherlands");

    await userEvent.click(screen.getByRole("button", { name: /place order/i }));

    await act(async () => {
      jest.advanceTimersByTime(5000);
    });

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        "/checkout",
        expect.objectContaining({ method: "POST" })
      );
    });

    const checkoutCall = global.fetch.mock.calls.find(([url]) =>
      String(url).endsWith("/checkout")
    );
    expect(JSON.parse(checkoutCall[1].body)).toEqual(
      expect.objectContaining({
        FirstName: "Mark",
        LastName: "Tester",
        Email: "mark.tester@example.com",
        PaymentMethod: "Apple Pay",
        Items: [
          {
            VariantId: 101,
            Name: "Slim Jeans",
            Price: 80,
            Quantity: 1
          }
        ]
      })
    );
    expect(localStorage.getItem("guestCart")).toBeNull();
  });
});
