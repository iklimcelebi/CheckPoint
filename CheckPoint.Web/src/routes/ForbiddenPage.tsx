import { Link } from 'react-router'
import { Page } from '../components/Page'

export function ForbiddenPage() {
  return (
    <Page title="Erişim reddedildi" description="Bu sayfayı görüntüleme yetkiniz yok.">
      <Link to="/" className="text-sm font-medium text-accent hover:underline">
        Kaynaklara dön
      </Link>
    </Page>
  )
}
