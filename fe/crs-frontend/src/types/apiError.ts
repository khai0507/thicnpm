

export interface ApiErrorResponse { 
  message?: string; 
  [field: string]: string | undefined; // truong hop loi validation, key la ten field 
} 
