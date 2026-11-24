-- =====================================================================
-- BACKTOSCHOOL - ESQUEMA POSTGRESQL
-- Orden de ejecución: 01_schema.sql, 02_seed_security.sql, 03_seed_catalog.sql
-- Las tablas de seguridad son las mismas de los demás sistemas M-BOS (CheckInvoice).
-- =====================================================================

CREATE TABLE permission (
    permission_id BIGSERIAL PRIMARY KEY,
    code VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    module VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_permission_code UNIQUE (code)
);


CREATE TABLE app_user (
    app_user_id BIGSERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    last_login TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP,
    updated_by BIGINT,
    CONSTRAINT uq_app_user_email UNIQUE (email),
    CONSTRAINT uq_app_user_username UNIQUE (username),
    CONSTRAINT fk_app_user_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT fk_app_user_updated_by FOREIGN KEY (updated_by) REFERENCES app_user(app_user_id)
);

-- =====================================================================
-- LEVEL 3: depend on app_user
-- =====================================================================


CREATE TABLE role (
    role_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP,
    updated_by BIGINT,
    CONSTRAINT uq_role_name UNIQUE (name),
    CONSTRAINT fk_role_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT fk_role_updated_by FOREIGN KEY (updated_by) REFERENCES app_user(app_user_id)
);

CREATE TABLE refresh_token (
    refresh_token_id BIGSERIAL PRIMARY KEY,
    app_user_id BIGINT NOT NULL,
    token VARCHAR(500) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_ip VARCHAR(50),
    CONSTRAINT fk_refresh_token_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id) ON DELETE CASCADE,
    CONSTRAINT uq_refresh_token_token UNIQUE (token)
);


CREATE TABLE audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    app_user_id BIGINT,
    table_name VARCHAR(100) NOT NULL,
    record_id BIGINT,
    log_date TIMESTAMP NOT NULL DEFAULT NOW(),
    action VARCHAR(20) NOT NULL,
    CONSTRAINT fk_audit_log_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id)
);

CREATE INDEX idx_audit_log_app_user ON audit_log(app_user_id);
CREATE INDEX idx_audit_log_table_name ON audit_log(table_name);

-- =====================================================================
-- LEVEL 4: app_user-role-permission relations, menu, church
-- =====================================================================

CREATE TABLE app_user_role (
    app_user_role_id BIGSERIAL PRIMARY KEY,
    app_user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    CONSTRAINT fk_app_user_role_app_user FOREIGN KEY (app_user_id) REFERENCES app_user(app_user_id) ON DELETE CASCADE,
    CONSTRAINT fk_app_user_role_role FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE,
    CONSTRAINT fk_app_user_role_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT uq_app_user_role UNIQUE (app_user_id, role_id)
);

CREATE INDEX idx_app_user_role_app_user ON app_user_role(app_user_id);
CREATE INDEX idx_app_user_role_role ON app_user_role(role_id);

CREATE TABLE role_permission (
    role_permission_id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT,
    CONSTRAINT fk_role_permission_role FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permission_permission FOREIGN KEY (permission_id) REFERENCES permission(permission_id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permission_created_by FOREIGN KEY (created_by) REFERENCES app_user(app_user_id),
    CONSTRAINT uq_role_permission UNIQUE (role_id, permission_id)
);

CREATE INDEX idx_role_permission_role ON role_permission(role_id);
CREATE INDEX idx_role_permission_permission ON role_permission(permission_id);

CREATE TABLE menu (
    menu_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    translation_key VARCHAR(100),
    route VARCHAR(150),
    icon VARCHAR(50),
    parent_menu_id BIGINT,
    display_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    permission_id BIGINT,
    CONSTRAINT fk_menu_parent FOREIGN KEY (parent_menu_id) REFERENCES menu(menu_id) ON DELETE CASCADE,
    CONSTRAINT fk_menu_permission FOREIGN KEY (permission_id) REFERENCES permission(permission_id),
    CONSTRAINT uq_menu_name UNIQUE (name)
);

CREATE INDEX idx_menu_parent ON menu(parent_menu_id);
CREATE INDEX idx_menu_permission ON menu(permission_id);

CREATE TABLE menu_role (
    menu_role_id BIGSERIAL PRIMARY KEY,
    menu_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_menu_role_menu FOREIGN KEY (menu_id) REFERENCES menu(menu_id) ON DELETE CASCADE,
    CONSTRAINT fk_menu_role_role FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE,
    CONSTRAINT uq_menu_role UNIQUE (menu_id, role_id)
);

CREATE INDEX idx_menu_role_menu ON menu_role(menu_id);
CREATE INDEX idx_menu_role_role ON menu_role(role_id);


-- =====================================================================
-- BACKTOSCHOOL: catálogo y pedidos
-- =====================================================================

CREATE TABLE school (
    school_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_school_name UNIQUE (name)
);

CREATE TABLE course (
    course_id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    level VARCHAR(20) NOT NULL,              -- INICIAL, PRIMARIA, SECUNDARIA
    display_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_course_name UNIQUE (name)
);

CREATE TABLE product (
    product_id BIGSERIAL PRIMARY KEY,
    code VARCHAR(30),                        -- código del inventario; opcional
    name VARCHAR(250) NOT NULL,
    section VARCHAR(20) NOT NULL,            -- TEXT, NOTEBOOK, UNIFORM, OTHER
    account_group VARCHAR(100),              -- grupo contable del inventario, solo referencia
    garment_name VARCHAR(150),               -- uniformes: la prenda (BLUSA, BUSO DEPORTIVO...)
    size VARCHAR(20),                        -- uniformes: la talla
    size_order INT NOT NULL DEFAULT 0,
    price NUMERIC(12,2) NOT NULL DEFAULT 0,
    unit_cost NUMERIC(12,2),
    stock NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    CONSTRAINT ck_product_section CHECK (section IN ('TEXT', 'NOTEBOOK', 'UNIFORM', 'OTHER')),
    CONSTRAINT ck_product_price CHECK (price >= 0)
);

CREATE UNIQUE INDEX uq_product_code ON product(code) WHERE code IS NOT NULL;
CREATE UNIQUE INDEX uq_product_name_without_code ON product(name) WHERE code IS NULL;
CREATE INDEX idx_product_section ON product(section);

CREATE TABLE course_product (
    course_product_id BIGSERIAL PRIMARY KEY,
    course_id BIGINT NOT NULL,
    product_id BIGINT NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    is_default_selected BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_course_product_course FOREIGN KEY (course_id) REFERENCES course(course_id) ON DELETE CASCADE,
    CONSTRAINT fk_course_product_product FOREIGN KEY (product_id) REFERENCES product(product_id) ON DELETE CASCADE,
    CONSTRAINT uq_course_product UNIQUE (course_id, product_id)
);

-- Número de ficha del día: se incrementa con INSERT ... ON CONFLICT DO UPDATE (atómico).
CREATE TABLE ticket_counter (
    ticket_date DATE PRIMARY KEY,
    last_number INT NOT NULL
);

CREATE TABLE sale_order (
    sale_order_id BIGSERIAL PRIMARY KEY,
    ticket_number INT NOT NULL,
    ticket_date DATE NOT NULL,
    customer_name VARCHAR(200) NOT NULL,
    tax_id VARCHAR(30),
    phone VARCHAR(30),
    school_id BIGINT,
    school_name VARCHAR(150),
    promoter_app_user_id BIGINT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',   -- PENDING, PRINTED, READY, CANCELLED
    total NUMERIC(12,2) NOT NULL DEFAULT 0,
    notes VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    printed_at TIMESTAMP,
    ready_at TIMESTAMP,
    cancelled_at TIMESTAMP,
    cancel_reason VARCHAR(250),
    CONSTRAINT fk_sale_order_school FOREIGN KEY (school_id) REFERENCES school(school_id),
    CONSTRAINT fk_sale_order_promoter FOREIGN KEY (promoter_app_user_id) REFERENCES app_user(app_user_id),
    CONSTRAINT uq_sale_order_ticket UNIQUE (ticket_date, ticket_number),
    CONSTRAINT ck_sale_order_status CHECK (status IN ('PENDING', 'PRINTED', 'READY', 'CANCELLED'))
);

CREATE INDEX idx_sale_order_date_status ON sale_order(ticket_date, status);
CREATE INDEX idx_sale_order_promoter ON sale_order(promoter_app_user_id);

CREATE TABLE sale_order_line (
    sale_order_line_id BIGSERIAL PRIMARY KEY,
    sale_order_id BIGINT NOT NULL,
    product_id BIGINT,                       -- NULL: texto escrito a mano por el promotor
    code VARCHAR(30),
    description VARCHAR(250) NOT NULL,
    size VARCHAR(20),
    section VARCHAR(20) NOT NULL,
    course_id BIGINT,
    course_name VARCHAR(100),
    student_label VARCHAR(100),
    quantity INT NOT NULL,
    unit_price NUMERIC(12,2) NOT NULL,
    subtotal NUMERIC(12,2) NOT NULL,
    is_manual BOOLEAN NOT NULL DEFAULT FALSE,
    display_order INT NOT NULL DEFAULT 0,
    CONSTRAINT fk_sale_order_line_order FOREIGN KEY (sale_order_id) REFERENCES sale_order(sale_order_id) ON DELETE CASCADE,
    CONSTRAINT fk_sale_order_line_product FOREIGN KEY (product_id) REFERENCES product(product_id),
    CONSTRAINT fk_sale_order_line_course FOREIGN KEY (course_id) REFERENCES course(course_id),
    CONSTRAINT ck_sale_order_line_quantity CHECK (quantity > 0)
);

CREATE INDEX idx_sale_order_line_order ON sale_order_line(sale_order_id);
