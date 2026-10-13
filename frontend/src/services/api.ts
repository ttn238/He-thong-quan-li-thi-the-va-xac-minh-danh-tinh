// Axios or native fetch based API client with JWT interceptor and error handling

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7196/api/v1';

export interface ApiResponse<T = any> {
  data?: T;
  message?: string;
  error?: string;
}

export class ApiClient {
  private static getToken(): string | null {
    return localStorage.getItem('access_token');
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
      localStorage.removeItem('access_token');
      localStorage.removeItem('current_user');
      window.dispatchEvent(new Event('auth:unauthorized'));
      throw new Error('Phiên đăng nhập đã hết hạn hoặc không hợp lệ.');
    }

    if (response.status === 403) {
      throw new Error('Bạn không có quyền thực hiện thao tác này.');
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
      throw new Error(errorMessage);
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

export default ApiClient;
