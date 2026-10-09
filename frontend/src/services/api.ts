export class ApiError extends Error {
    constructor(public status: number, message: string) {
        super(message);
    }
}

export async function post<T>(url: string, body: unknown): Promise<T> {
    let res: Response;
    try {
        res = await fetch(`/api/v1${url}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });
    } catch {
        throw new ApiError(0, 'Không kết nối được máy chủ.');
    }
    const data = await res.json().catch(() => null);
    if (!res.ok) throw new ApiError(res.status, data?.message ?? 'Có lỗi xảy ra.');
    return data as T;
}