// src/api/products.ts
// Example service — copy this pattern for other entities

import apiClient from "./client";

export interface ProductVariant {
  variantId: number;
  sizeId: number;
  colorId: number;
  pictureUrl: string;
  stock: number;
}

export interface Product {
  productId: number;
  name: string;
  description: string;
  brand: string;
  basePrice: number;
  isActive: boolean;
  categoryId: number;
  variants: ProductVariant[];
}

export const ProductsApi = {
  getAll: () =>
    apiClient.get<Product[]>("/api/products").then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<Product>(`/api/products/${id}`).then((r) => r.data),

  create: (product: Omit<Product, "productId" | "variants">) =>
    apiClient.post<Product>("/api/products", product).then((r) => r.data),

  update: (id: number, product: Partial<Product>) =>
    apiClient.put<Product>(`/api/products/${id}`, product).then((r) => r.data),

  delete: (id: number) =>
    apiClient.delete(`/api/products/${id}`).then((r) => r.data),
};