import { createBrowserRouter, Navigate } from 'react-router'
import { AppLayout } from '../components/AppLayout'
import { AuthLayout } from '../components/AuthLayout'
import { PlaceholderPage } from '../components/PlaceholderPage'
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
              {
                path: '/giris',
                lazy: async () => ({
                  Component: (await import('../features/auth/LoginPage')).LoginPage,
                }),
              },
              {
                path: '/kayit',
                lazy: async () => ({
                  Component: (await import('../features/auth/RegisterPage')).RegisterPage,
                }),
              },
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
                lazy: async () => ({
                  Component: (await import('../features/resources/ResourceListPage'))
                    .ResourceListPage,
                }),
              },
              {
                path: '/kaynaklar/:id',
                lazy: async () => ({
                  Component: (await import('../features/resources/ResourceDetailPage'))
                    .ResourceDetailPage,
                }),
              },
              {
                path: '/kaynaklar/:id/duzenle',
                element: (
                  <PlaceholderPage
                    title="Kaynağı düzenle"
                    description="Başlık, açıklama ve türü güncelleyin."
                  />
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
                lazy: async () => ({
                  Component: (await import('../features/resources/MyResourcesPage'))
                    .MyResourcesPage,
                }),
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
              {
                path: '/profil',
                lazy: async () => ({
                  Component: (await import('../features/auth/ProfilePage')).ProfilePage,
                }),
              },
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
