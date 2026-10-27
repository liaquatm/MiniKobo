// TypeScript mirrors of the JSON the .NET APIs return (camelCase, like ASP.NET Core sends it).

export type Book = {
  isbn: string;
  title: string;
  author: string;
  description: string;
  price: number;
  stockQuantity: number;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type CartItem = {
  isbn: string;
  title: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
};

export type Cart = {
  id: string;
  items: CartItem[];
  total: number;
};

// RFC 9457 ProblemDetails, the error shape both APIs use.
export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};
