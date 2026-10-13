import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError } from '../../services/api';
import { authService } from '../../services/authService';

const DON_VI = [
    'Trung tâm Giám định Pháp y Quốc gia - Phòng Hồ sơ DVI',
    'Công an TP. Thủ Đức',
    'Công an Quận 1',
    'Nhà xác Bệnh viện Chợ Rẫy',
];
const LAST_USER = 'qltldt.lastUser';

// Ngôi sao 5 cánh của logo
const STAR = Array.from({ length: 10 }, (_, i) => {
    const a = -Math.PI / 2 + (i * Math.PI) / 5;
    const r = i % 2 === 0 ? 9 : 3.8;
    return `${(32 + r * Math.cos(a)).toFixed(2)},${(31 + r * Math.sin(a)).toFixed(2)}`;
}).join(' ');

export default function Login() {
    const nav = useNavigate();
    const [user, setUser] = useState(() => localStorage.getItem(LAST_USER) ?? '');
    const [pass, setPass] = useState('');
    const [showPass, setShowPass] = useState(false);
    const [donVi, setDonVi] = useState(DON_VI[0]);
    const [nho, setNho] = useState(true);
    const [loi, setLoi] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    async function submit(e: FormEvent) {
        e.preventDefault();
        if (!user.trim() || !pass) {
            setLoi('Vui lòng nhập tên đăng nhập và mật khẩu.');
            return;
        }
        setBusy(true);
        setLoi(null);
        try {
            const tk = await authService.login(user.trim(), pass, donVi);
            if (nho) localStorage.setItem(LAST_USER, tk.tenDangNhap);
            else localStorage.removeItem(LAST_USER);
            nav('/dashboard', { replace: true });
        } catch (err) {
            setLoi(err instanceof ApiError ? err.message : 'Có lỗi xảy ra.');
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className= "lg-page" >
        <header className="lg-top" >
            <div className="lg-row" >
                <span className="lg-mono" > SECURE - GATEWAY // DVI-NATIONAL-IDENTIFICATION-TERMINAL </span>
                    <span className = "lg-mono lg-green"> ● TRẠM TRỰC TUYẾN · NODE: VN - HAN - DVI-04 </span>
                        </div>
                        <nav>
    {
        ['Hệ thống', 'Giám định hồ sơ', 'Đồng bộ ADN & Sinh trắc', 'Tra cứu chuỗi lưu ký', 'Báo cáo & Thống kê', 'Trợ giúp (F1)'].map(
            (m, i) => (
                <span key= { m } className = { i === 0 ? 'on' : ''}> { m } </span>
            ),
          )
}
</nav>
    </header>

    < main className = "lg-main" >
        <form className="lg-card" onSubmit = { submit } >
            <div className="lg-title" > Cổng Đăng Nhập Hệ Thống Quản Trị - Phiên bản 4.2.0 - PROD </div>

                < div className = "lg-body" >
                    <svg className="lg-logo" viewBox = "0 0 64 64" width = "56" height = "56" >
                        <rect width="64" height = "64" rx = "10" fill = "#eef2f7" />
                            <polygon points="32,9 51,16 51,30 46,42 32,53 18,42 13,30 13,16" fill = "#0f2b4d" />
                                <polygon points="32,15 45,20 45,30 41,39 32,47 23,39 19,30 19,20" fill = "none" stroke = "#fff" strokeWidth = "1.6" />
                                    <polygon points={ STAR } fill = "#f2c94c" />
                                        </svg>
                                        < h1 > HỆ THỐNG QUẢN LÝ THI THỂ VÀ XÁC MINH DANH TÍNH </h1>
                                            < p className = "lg-sub" >
                                                Phần mềm quản lý hồ sơ tử thi, giám định ADN và quy trình lưu ký quốc gia(DVI)
                                                    </p>

{ loi && <div className="lg-alert" role = "alert" > { loi } </div> }

<label className="lg-label" htmlFor = "lg-user" >
    <span>TÊN ĐĂNG NHẬP / MÃ CÔNG VỤ < b className = "req" >* </b></span >
        <small>Mã cán bộ / Thẻ ngành </small>
            </label>
            < div className = "lg-input" >
                <span>👤</span>
                    < input id = "lg-user" value = { user } onChange = {(e) => setUser(e.target.value)}
placeholder = "cstt.admin_dvi" autoComplete = "username" />
    </div>

    < label className = "lg-label" htmlFor = "lg-pass" >
        <span>MẬT KHẨU BẢO MẬT < b className = "req" >* </b></span >
            <small>Cấp phát bởi BCA / BYT </small>
                </label>
                < div className = "lg-input" >
                    <span>🔒</span>
                        < input id = "lg-pass" type = { showPass? 'text': 'password' } value = { pass }
                        onChange = {(e) => setPass(e.target.value)} placeholder = "••••••••••••"
                        autoComplete = "current-password" />
                        <button type="button" className = "lg-eye" onClick = {() => setShowPass((v) => !v)}>👁</button>
    </div>

    < label className = "lg-label" htmlFor = "lg-dv" > <span>ĐƠN VỊ / CHI CỤC LƯU HỒ SƠ </span></label >
        <div className="lg-input" >
            <span>🏢</span>
                < select id = "lg-dv" value = { donVi } onChange = {(e) => setDonVi(e.target.value)}>
                { DON_VI.map((d) => <option key={ d } > { d } </option>) }
                    </select>
                    </div>

                    < div className = "lg-help" style = {{ marginTop: 12 }}>
                        <label style={ { fontSize: 12, color: 'var(--navy)' } }>
                            <input type="checkbox" checked = { nho } onChange = {(e) => setNho(e.target.checked)} />{' '}
                Ghi nhớ phiên làm việc trên máy trạm
    </label>
    < button type = "button" className = "lg-link"
onClick = {() => window.alert('Vui lòng liên hệ quản trị viên để được cấp lại mật khẩu.')}>
    Quên mật khẩu ?
        </button>
        </div>

        < button className = "lg-btn" disabled = { busy } >
        { busy? 'ĐANG XÁC THỰC...': '🛡  XÁC THỰC VÀ ĐĂNG NHẬP' }
            </button>
            < button type = "button" className = "lg-pki"
onClick = {() => window.alert('Chức năng đăng nhập bằng thẻ PKI / USB Token chưa được triển khai.')}>
              🔌  ĐĂNG NHẬP BẰNG THẺ PKI / TOKEN CHUYÊN DỤNG(USB CRYPTO)
    </button>

    < div className = "lg-warn" >
        <span>🛡</span>
            < p >
            <b>CẢNH BÁO BẢO MẬT PHÁP LÝ: </b>
                Hệ thống lưu trữ dữ liệu nhân thân và nhận dạng cấp quốc gia.Mọi hành vi truy cập trái phép
                hoặc sao chép hồ sơ lưu ký từ thi thể đều bị ghi nhật ký và xử lý theo Luật Bảo vệ bí mật nhà nước.
              </p>
    </div>

    < div className = "lg-help" >
        <span>🎧 Trực ban giám sát: 1900 -0909(Nhánh 2) </span>
            < span style = {{ color: 'var(--blue)' }}> Tài liệu quy chuẩn(PDF) </span>
                </div>
                </div>

                < div className = "lg-status" >
                    <span>TLS 1.3 / AES - 256 GCM | MÁY TRẠM: 10.24.88.192(LAN AN NINH) </span>
                        < span > PHIÊN: SEC - VN - 9982 </span>
                            </div>
                            </form>
                            </main>

                            < footer className = "lg-bottom" >
                                <span className="lg-mono" >● CƠ QUAN CHỦ QUẢN: LIÊN BỘ Y TẾ & BỘ CÔNG AN VIỆT NAM </span>
                                    < span > TRUNG TÂM DỮ LIỆU ĐỊNH DANH NẠN NHÂN THIÊN TAI VÀ THẢM HỌA(DVI VIETNAM) </span>
                                        </footer>
                                        </div>
  );
}