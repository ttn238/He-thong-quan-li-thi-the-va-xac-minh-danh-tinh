// API client with JWT interceptor, Vite env support, and unified auth storage

export class ApiError extends Error {
  public status?: number;
  public data?: any;
  constructor(message: string, status?: number, data?: any) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.data = data;
  }
}

const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:5103/api/v1';

export const AUTH_TOKEN_KEY = 'access_token';
export const AUTH_USER_KEY = 'current_user';
export const AUTH_SESSION_KEY = 'qltldt.session';

export interface ApiResponse<T = any> {
  data?: T;
  message?: string;
  error?: string;
}

export class ApiClient {
  public static getToken(): string | null {
    return localStorage.getItem(AUTH_TOKEN_KEY) || sessionStorage.getItem(AUTH_TOKEN_KEY);
  }

  public static setToken(token: string): void {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
    sessionStorage.setItem(AUTH_TOKEN_KEY, token);
  }

  public static clearAuth(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USER_KEY);
    localStorage.removeItem(AUTH_SESSION_KEY);
    sessionStorage.removeItem(AUTH_TOKEN_KEY);
    sessionStorage.removeItem(AUTH_USER_KEY);
    sessionStorage.removeItem(AUTH_SESSION_KEY);
  }

  public static async request<T = any>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = this.getToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const url = endpoint.startsWith('http') ? endpoint : `${API_BASE_URL}${endpoint}`;

    const response = await fetch(url, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.clearAuth();
      window.dispatchEvent(new Event('auth:unauthorized'));
      throw new Error('Phiên đăng nhập đã hết hạn hoặc không hợp lệ.');
    }

    if (response.status === 403) {
      throw new Error('Bạn không có quyền thực hiện thao tác này (Yêu cầu vai trò Quản trị viên).');
    }

    if (!response.ok) {
      let errorMessage = `Yêu cầu thất bại với mã lỗi ${response.status}`;
      try {
        const errorData = await response.json();
        if (errorData?.message) {
          errorMessage = errorData.message;
        } else if (errorData?.title) {
          errorMessage = errorData.title;
        }
      } catch {
        if (response.statusText) errorMessage = response.statusText;
      }
      throw new Error(errorMessage);
    }

    const contentLength = response.headers.get('content-length');
    if (contentLength === '0' || response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  public static get<T = any>(endpoint: string, params?: Record<string, any>): Promise<T> {
    let url = endpoint;
    if (params) {
      const searchParams = new URLSearchParams();
      Object.entries(params).forEach(([key, val]) => {
        if (val !== undefined && val !== null && val !== '') {
          searchParams.append(key, String(val));
        }
      });
      const queryString = searchParams.toString();
      if (queryString) {
        url += (url.includes('?') ? '&' : '?') + queryString;
      }
    }
    return this.request<T>(url, { method: 'GET' });
  }

  public static post<T = any>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  public static put<T = any>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  public static delete<T = any>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}

// Function exports matching import statements in other files
export const get = <T = any>(endpoint: string, params?: Record<string, any>): Promise<T> =>
  ApiClient.get<T>(endpoint, params);

export const post = <T = any>(endpoint: string, data?: any): Promise<T> =>
  ApiClient.post<T>(endpoint, data);

export const put = <T = any>(endpoint: string, data?: any): Promise<T> =>
  ApiClient.put<T>(endpoint, data);

export const del = <T = any>(endpoint: string): Promise<T> =>
  ApiClient.delete<T>(endpoint);

export default ApiClient;
