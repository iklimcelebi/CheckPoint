import { createBrowserRouter } from 'react-router'
import { AppLayout } from '../components/AppLayout'
import { AuthLayout } from '../components/AuthLayout'
import { GuestOnly, RequireAdmin, RequireAuth } from './guards'
import { NotFoundPage } from './NotFoundPage'
import { PageLoading } from './PageLoading'
import { RouteErrorPage } from './RouteErrorPage'

export const router = createBrowserRouter([
  {
    errorElement: <RouteErrorPage />,
    hydrateFallbackElement: <PageLoading />,
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
              {
                path: '/',
                lazy: async () => ({
                  Component: (await import('../features/home/HomePage')).HomePage,
                }),
              },
              {
                path: '/arama',
                lazy: async () => ({
                  Component: (await import('../features/search/SearchPage')).SearchPage,
                }),
              },
              {
                path: '/favoriler',
                lazy: async () => ({
                  Component: (await import('../features/resources/FavoritesPage')).FavoritesPage,
                }),
              },
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
                lazy: async () => ({
                  Component: (await import('../features/resources/EditResourcePage'))
                    .EditResourcePage,
                }),
              },
              {
                path: '/yukle',
                lazy: async () => ({
                  Component: (await import('../features/resources/UploadPage')).UploadPage,
                }),
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
                lazy: async () => ({
                  Component: (await import('../features/universities/UniversitiesPage'))
                    .UniversitiesPage,
                }),
              },
              {
                path: '/dersler',
                lazy: async () => ({
                  Component: (await import('../features/courses/CoursesPage')).CoursesPage,
                }),
              },
              {
                path: '/profil/parola',
                lazy: async () => ({
                  Component: (await import('../features/auth/ChangePasswordPage'))
                    .ChangePasswordPage,
                }),
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
                    lazy: async () => ({
                      Component: (await import('../features/admin/AdminPage')).AdminPage,
                    }),
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
