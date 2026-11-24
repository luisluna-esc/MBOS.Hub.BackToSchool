-- =====================================================================
-- Borra TODOS los pedidos y reinicia el número de ficha. Solo para dejar la base
-- limpia después de pruebas o de una demostración. NO usar en producción.
-- No toca el catálogo, los precios, las listas por curso ni los usuarios.
-- =====================================================================
BEGIN;

-- El stock que bajó por pedidos marcados "Listo en caja" se devuelve antes de borrarlos.
UPDATE product p
SET stock = p.stock + x.quantity
FROM (
    SELECT l.product_id, SUM(l.quantity) AS quantity
    FROM sale_order_line l
    JOIN sale_order o ON o.sale_order_id = l.sale_order_id
    WHERE o.status = 'READY' AND l.product_id IS NOT NULL
    GROUP BY l.product_id
) x
WHERE p.product_id = x.product_id;

DELETE FROM sale_order_line;
DELETE FROM sale_order;
DELETE FROM ticket_counter;

COMMIT;
