import { useNavigate } from 'react-router-dom';
import { authService } from '../../services/authService';

export default function UserDashboard() {
    const nav = useNavigate();
    const s = authService.getSession();
    if (!s) return null;
    return (
        <div style= {{ padding: 30, fontSize: 16, lineHeight: 1.7 }
}>
    <b>Đăng nhập thành công </b>
        < div > Xin chào: { s.user.hoTen } </div>
            < div > Vai trò: { s.user.tenVaiTro } </div>
                < div > Đơn vị: { s.donVi } </div>
                    < button
style = {{ marginTop: 20, padding: '8px 20px' }}
onClick = {() => { authService.logout(); nav('/login'); }}
      >
    ĐĂNG XUẤT
        </button>
        </div>
  );
}