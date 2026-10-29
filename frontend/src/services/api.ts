// Máy khách HTTP dùng chung. Giữ cả hai cách gọi mà các file khác đang dùng:
//   - ApiClient.get/post/put/delete   (userService, ...)
//   - post(...) và ApiError           (authService, Login)

// Địa chỉ gốc. Mặc định đi qua proxy của Vite (/api -> backend, /ai -> dịch vụ AI).
// Khi triển khai thật: đặt VITE_API_URL / VITE_AI_API_URL trong .env.production.
const API_BASE_URL: string = import.meta.env.VITE_API_URL ?? '/api/v1';
export const AI_API_URL: string = import.meta.env.VITE_AI_API_URL ?? '/ai';

const SESSION_KEY = 'qltldt.session';

export interface ApiResponse<T = any> {
    data?: T;
    message?: string;
    error?: string;
}

export class ApiError extends Error {
    constructor(public status: number, message: string) {
        super(message);
        this.name = 'ApiError';
    }
}

// JWT nằm trong phiên đăng nhập (authService lưu LoginResponse.Token)
function getToken(): string | null {
    try {
        const raw = sessionStorage.getItem(SESSION_KEY);
        const token = raw ? JSON.parse(raw)?.user?.token : null;
        if (typeof token === 'string' && token) return token;
        return localStorage.getItem('access_token');
    } catch {
        return null;
    }
}

function clearSession(): void {
    try {
        sessionStorage.removeItem(SESSION_KEY);
        localStorage.removeItem('access_token');
        localStorage.removeItem('current_user');
    } catch {
        /* bỏ qua */
    }
    window.dispatchEvent(new Event('auth:unauthorized'));
}

async function readMessage(res: Response): Promise<string> {
    try {
        const d = await res.json();
        if (typeof d?.message === 'string') return d.message;   // backend .NET
        if (typeof d?.detail === 'string') return d.detail;     // FastAPI
        if (typeof d?.title === 'string') return d.title;       // ProblemDetails
    } catch {
        /* không có JSON */
    }
    if (res.status >= 500) return `Máy chủ không phản hồi hoặc đang gặp sự cố (HTTP ${res.status}).`;
    return res.statusText || `Yêu cầu thất bại (mã lỗi ${res.status}).`;
}

export class ApiClient {
    public static async request<T = any>(
        endpoint: string,
        options: RequestInit = {},
        baseUrl: string = API_BASE_URL,
    ): Promise<T> {
        const token = getToken();
        const headers: Record<string, string> = {
            Accept: 'application/json',
            ...(options.headers as Record<string, string>),
        };
        // Gửi file (FormData) thì để trình duyệt tự đặt Content-Type kèm boundary
        if (!(options.body instanceof FormData)) headers['Content-Type'] = 'application/json';
        if (token) headers['Authorization'] = `Bearer ${token}`;

        const url = endpoint.startsWith('http') ? endpoint : `${baseUrl}${endpoint}`;

        let res: Response;
        try {
            res = await fetch(url, { ...options, headers });
        } catch {
            throw new ApiError(0, 'Không kết nối được máy chủ.');
        }

        if (res.status === 401) {
            // Đang có phiên mà bị 401 = hết hạn. Đăng nhập sai thì giữ thông báo của máy chủ.
            if (token && !endpoint.startsWith('/auth/login')) {
                clearSession();
                throw new ApiError(401, 'Phiên đăng nhập đã hết hạn hoặc không hợp lệ.');
            }
            throw new ApiError(401, await readMessage(res));
        }
        if (res.status === 403) throw new ApiError(403, 'Bạn không có quyền thực hiện thao tác này.');
        if (!res.ok) throw new ApiError(res.status, await readMessage(res));

        if (res.status === 204 || res.headers.get('content-length') === '0') return {} as T;
        return (await res.json()) as T;
    }

    public static get<T = any>(endpoint: string, params?: Record<string, any>, baseUrl?: string): Promise<T> {
        let url = endpoint;
        if (params) {
            const sp = new URLSearchParams();
            Object.entries(params).forEach(([k, v]) => {
                if (v !== undefined && v !== null && v !== '') sp.append(k, String(v));
            });
            const qs = sp.toString();
            if (qs) url += (url.includes('?') ? '&' : '?') + qs;
        }
        return this.request<T>(url, { method: 'GET' }, baseUrl);
    }

    public static post<T = any>(endpoint: string, data?: unknown, baseUrl?: string): Promise<T> {
        const body = data === undefined ? undefined : data instanceof FormData ? data : JSON.stringify(data);
        return this.request<T>(endpoint, { method: 'POST', body }, baseUrl);
    }

    public static put<T = any>(endpoint: string, data?: unknown, baseUrl?: string): Promise<T> {
        const body = data === undefined ? undefined : JSON.stringify(data);
        return this.request<T>(endpoint, { method: 'PUT', body }, baseUrl);
    }

    public static delete<T = any>(endpoint: string, baseUrl?: string): Promise<T> {
        return this.request<T>(endpoint, { method: 'DELETE' }, baseUrl);
    }
}

// Dạng hàm rời, authService đang import `post`
export const get = <T = any>(endpoint: string, params?: Record<string, any>) => ApiClient.get<T>(endpoint, params);
export const post = <T = any>(endpoint: string, data?: unknown) => ApiClient.post<T>(endpoint, data);
export const put = <T = any>(endpoint: string, data?: unknown) => ApiClient.put<T>(endpoint, data);
export const del = <T = any>(endpoint: string) => ApiClient.delete<T>(endpoint);

export default ApiClient;