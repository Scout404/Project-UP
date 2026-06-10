import { render, screen, waitFor } from "@testing-library/react";
import App from "../App";

jest.mock("../AdminPage", () => function MockAdminPanel({ user }) {
  return <div>Admin dashboard for {user?.username}</div>;
});

jest.mock("../CustomerHome", () => function MockCustomerHome({ user }) {
  return <div>{user ? `Customer home for ${user.username}` : "Guest customer home"}</div>;
});

describe("role based routing", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  test("renders the admin dashboard when the stored user has the Admin role", async () => {
    localStorage.setItem(
      "user",
      JSON.stringify({ id: 1, username: "admin", role: "Admin" })
    );

    render(<App />);

    expect(await screen.findByText("Admin dashboard for admin")).toBeInTheDocument();
    expect(screen.queryByText(/Customer home/i)).not.toBeInTheDocument();
  });

  test("renders the customer shop when the stored user is not an admin", async () => {
    localStorage.setItem(
      "user",
      JSON.stringify({ id: 2, username: "testuser", role: "Customer" })
    );

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText("Customer home for testuser")).toBeInTheDocument();
    });
    expect(screen.queryByText(/Admin dashboard/i)).not.toBeInTheDocument();
  });
});
