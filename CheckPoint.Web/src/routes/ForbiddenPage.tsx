import { Link } from 'react-router'
import { Page } from '../components/Page'

export function ForbiddenPage() {
  return (
    <Page title="Erişim reddedildi" description="Bu sayfayı görüntüleme yetkiniz yok.">
      <Link to="/kaynaklar" className="text-sm font-medium text-blue-700 hover:underline">
        Kaynaklara dön
      </Link>
    </Page>
  )
}
