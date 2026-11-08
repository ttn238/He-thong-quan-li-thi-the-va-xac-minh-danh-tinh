// Native fetch based API client with JWT interceptor and error handling
import { session } from './session';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7196/api/v1';

export class ApiError extends Error {
  constructor(message: string, public status = 0) {
    super(message);
    this.name = 'ApiError';
  }
}

export interface ApiResponse<T = any> {
  data?: T;
  message?: string;
  error?: string;
}

export class ApiClient {
  private static getToken(): string | null {
    return session.get()?.token ?? null;
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

    let response: Response;
    try {
      response = await fetch(url, { ...options, headers });
    } catch {
      throw new ApiError('Không kết nối được máy chủ.');
    }

    // 401 khi đang có phiên = hết hạn/không hợp lệ => xóa phiên + báo cho app đưa về đăng nhập.
    // 401 ở chính API đăng nhập = sai thông tin, giữ nguyên thông báo từ server.
    if (response.status === 401 && token) {
      session.clear();
      window.dispatchEvent(new Event('auth:unauthorized'));
      throw new ApiError('Phiên đăng nhập đã hết hạn hoặc không hợp lệ.', 401);
    }

    if (response.status === 403) {
      throw new ApiError('Bạn không có quyền thực hiện thao tác này.', 403);
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
        // Fallback to status text
        if (response.statusText) errorMessage = response.statusText;
      }
      throw new ApiError(errorMessage, response.status);
    }

    // Check if response has content
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

export const post = <T = any>(endpoint: string, data?: any) => ApiClient.post<T>(endpoint, data);

export default ApiClient;
