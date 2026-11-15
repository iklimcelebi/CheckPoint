import { createBrowserRouter, Navigate } from 'react-router'
import { AppLayout } from '../components/AppLayout'
import { AuthLayout } from '../components/AuthLayout'
import { PlaceholderPage } from '../components/PlaceholderPage'
import { LoginPage } from '../features/auth/LoginPage'
import { ProfilePage } from '../features/auth/ProfilePage'
import { RegisterPage } from '../features/auth/RegisterPage'
import { GuestOnly, RequireAdmin, RequireAuth } from './guards'
import { NotFoundPage } from './NotFoundPage'
import { RouteErrorPage } from './RouteErrorPage'

export const router = createBrowserRouter([
  {
    errorElement: <RouteErrorPage />,
    children: [
      {
        element: <GuestOnly />,
        children: [
          {
            element: <AuthLayout />,
            children: [
              { path: '/giris', element: <LoginPage /> },
              { path: '/kayit', element: <RegisterPage /> },
            ],
          },
        ],
      },
      {
        element: <RequireAuth />,
        children: [
          {
            element: <AppLayout />,
            children: [
              { path: '/', element: <Navigate to="/kaynaklar" replace /> },
              {
                path: '/kaynaklar',
                element: (
                  <PlaceholderPage
                    title="Kaynaklar"
                    description="Üniversite, ders ve türe göre paylaşılan kaynaklar."
                  />
                ),
              },
              {
                path: '/kaynaklar/:id',
                element: (
                  <PlaceholderPage title="Kaynak ayrıntısı" description="Kaynağın tüm bilgileri." />
                ),
              },
              {
                path: '/yukle',
                element: (
                  <PlaceholderPage title="Kaynak yükle" description="Yeni bir kaynak paylaşın." />
                ),
              },
              {
                path: '/benim-kaynaklarim',
                element: (
                  <PlaceholderPage
                    title="Benim Kaynaklarım"
                    description="Yüklediğiniz kaynaklar."
                  />
                ),
              },
              {
                path: '/universiteler',
                element: (
                  <PlaceholderPage title="Üniversiteler" description="Kayıtlı üniversiteler." />
                ),
              },
              {
                path: '/dersler',
                element: (
                  <PlaceholderPage title="Dersler" description="Üniversitelere göre dersler." />
                ),
              },
              { path: '/profil', element: <ProfilePage /> },
              {
                element: <RequireAdmin />,
                children: [
                  {
                    path: '/yonetim',
                    element: (
                      <PlaceholderPage
                        title="Yönetim"
                        description="Yalnızca yöneticilerin erişebildiği işlemler."
                      />
                    ),
                  },
                ],
              },
            ],
          },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
