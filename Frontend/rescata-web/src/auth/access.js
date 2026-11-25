export const roleNames = { Business: 'Negocio', Foundation: 'Fundación', Admin: 'Administración' }

export const areas = [
  { path: '/negocio', title: 'Negocio', roles: ['Business', 'Admin'],
    actions: ['Publicar y cancelar donaciones', 'Confirmar y completar recogidas'] },
  { path: '/fundacion', title: 'Fundación', roles: ['Foundation', 'Admin'],
    actions: ['Programar recogidas', 'Reprogramar y cancelar recogidas'] },
  { path: '/administracion', title: 'Administración', roles: ['Admin'],
    actions: ['Consultar usuarios', 'Supervisar donaciones y recogidas'] },
]

export function canAccess(area, session) {
  return area.roles.some(role => session.roles.includes(role))
}
