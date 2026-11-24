-- =====================================================================
-- Seguridad de BackToSchool: permisos, roles, usuarios de prueba y menú.
-- Mismo modelo que CheckInvoice: dos permisos ("visita" = solo ver, "trabajo" = todo);
-- lo que cada rol ve lo decide el menú (menu_role) y, en la API, los [Authorize(Roles=...)].
-- =====================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;  -- crypt()/gen_salt('bf') = hashes BCrypt que la API valida

INSERT INTO permission (code, name, description, module) VALUES
('visita', 'Visita (solo ver)', 'Puede ver todo el sistema, pero no crear, editar ni eliminar nada.', 'General'),
('trabajo', 'Trabajo (acceso completo)', 'Acceso completo: puede ver, crear, editar y eliminar según su rol.', 'General')
ON CONFLICT (code) DO NOTHING;

INSERT INTO role (name, description, is_active) VALUES
('M-BOS', 'Superusuario', TRUE),
('Administrador', 'Encargado de la venta: precios, listas por curso, colegios y control de pedidos', TRUE),
('Promotor', 'Toma los pedidos de los papás desde el celular', TRUE),
('Almacén', 'Recibe los pedidos, imprime la hoja y prepara los materiales', TRUE),
('Caja', 'Consulta los pedidos por número de ficha para facturar', TRUE)
ON CONFLICT (name) DO NOTHING;

INSERT INTO role_permission (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM role r JOIN permission p ON p.code = CASE WHEN r.name = 'Caja' THEN 'visita' ELSE 'trabajo' END
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ---------------------------------------------------------------------
-- Usuarios de prueba (cambiar las contraseñas antes de producción):
--   mbos / mbos123          admin / admin123         promotor1 / promotor123
--   promotor2 / promotor123 almacen / almacen123     caja / caja123
-- ---------------------------------------------------------------------
INSERT INTO app_user (first_name, last_name, email, username, password_hash, is_active) VALUES
('M-BOS', 'Sistemas', 'mbos@backtoschool.local', 'mbos', crypt('mbos123', gen_salt('bf', 11)), TRUE),
('Encargado', 'Ventas', 'admin@backtoschool.local', 'admin', crypt('admin123', gen_salt('bf', 11)), TRUE),
('Promotor', 'Uno', 'promotor1@backtoschool.local', 'promotor1', crypt('promotor123', gen_salt('bf', 11)), TRUE),
('Promotor', 'Dos', 'promotor2@backtoschool.local', 'promotor2', crypt('promotor123', gen_salt('bf', 11)), TRUE),
('Almacén', 'Central', 'almacen@backtoschool.local', 'almacen', crypt('almacen123', gen_salt('bf', 11)), TRUE),
('Caja', 'Principal', 'caja@backtoschool.local', 'caja', crypt('caja123', gen_salt('bf', 11)), TRUE)
ON CONFLICT (username) DO NOTHING;

INSERT INTO app_user_role (app_user_id, role_id)
SELECT u.app_user_id, r.role_id
FROM app_user u
JOIN role r ON r.name = CASE u.username
    WHEN 'mbos' THEN 'M-BOS'
    WHEN 'admin' THEN 'Administrador'
    WHEN 'promotor1' THEN 'Promotor'
    WHEN 'promotor2' THEN 'Promotor'
    WHEN 'almacen' THEN 'Almacén'
    WHEN 'caja' THEN 'Caja'
END
ON CONFLICT (app_user_id, role_id) DO NOTHING;

-- ---------------------------------------------------------------------
-- Menú lateral
-- ---------------------------------------------------------------------
INSERT INTO menu (name, translation_key, route, icon, parent_menu_id, display_order) VALUES
('Inicio', 'nav.home', '/', 'home', NULL, 1),
('Nuevo pedido', 'nav.newOrder', '/orders/new', 'cart-plus', NULL, 2),
('Pedidos del día', 'nav.orders', '/orders', 'clipboard-list', NULL, 3),
('Almacén', 'nav.warehouse', '/warehouse', 'box', NULL, 4),
('Catálogo', 'nav.catalog', NULL, 'layers', NULL, 10),
('Administración', 'nav.admin', NULL, 'key-minimalistic', NULL, 20)
ON CONFLICT (name) DO NOTHING;

INSERT INTO menu (name, translation_key, route, icon, parent_menu_id, display_order)
SELECT v.name, v.translation_key, v.route, v.icon, m.menu_id, v.display_order
FROM (VALUES
    ('Productos y precios', 'nav.products', '/catalog/products', 'product', 'Catálogo', 1),
    ('Listas por curso', 'nav.courses', '/catalog/courses', 'report', 'Catálogo', 2),
    ('Colegios', 'nav.schools', '/catalog/schools', 'church', 'Catálogo', 3),
    ('Usuarios', 'nav.users', '/admin/users', 'client', 'Administración', 1),
    ('Roles', 'nav.roles', '/admin/roles', 'settings', 'Administración', 2)
) AS v(name, translation_key, route, icon, parent, display_order)
JOIN menu m ON m.name = v.parent AND m.parent_menu_id IS NULL
ON CONFLICT (name) DO NOTHING;

-- Quién ve cada opción. Los grupos (Catálogo, Administración) se ocultan solos si no hay hijos visibles.
INSERT INTO menu_role (menu_id, role_id)
SELECT m.menu_id, r.role_id
FROM menu m
JOIN role r ON
    (m.route IN ('/', '/orders'))
    OR (m.route = '/orders/new' AND r.name IN ('M-BOS', 'Administrador', 'Promotor'))
    OR (m.route = '/warehouse' AND r.name IN ('M-BOS', 'Administrador', 'Almacén'))
    OR (m.route LIKE '/catalog/%' AND r.name IN ('M-BOS', 'Administrador'))
    OR (m.route LIKE '/admin/%' AND r.name = 'M-BOS')
ON CONFLICT (menu_id, role_id) DO NOTHING;
