CREATE TABLE IF NOT EXISTS marketplace_sales (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    offer_id INT UNSIGNED NOT NULL,
    buyer_id INT UNSIGNED NOT NULL,
    seller_id INT UNSIGNED NOT NULL,
    furni_id INT UNSIGNED NOT NULL,
    item_id INT UNSIGNED NOT NULL,
    limited_number INT UNSIGNED NOT NULL,
    limited_stack INT UNSIGNED NOT NULL,
    item_type TINYINT UNSIGNED NOT NULL,
    sprite_id INT UNSIGNED NOT NULL,
    total_price INT UNSIGNED NOT NULL,
    seller_amount INT UNSIGNED NOT NULL,
    sold_at DATETIME(6) NOT NULL,
    INDEX ix_marketplace_sales_offer (offer_id),
    INDEX ix_marketplace_sales_item (item_type, sprite_id, sold_at),
    INDEX ix_marketplace_sales_buyer (buyer_id, sold_at),
    INDEX ix_marketplace_sales_seller (seller_id, sold_at)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS marketplace_daily_sales (
    item_type TINYINT UNSIGNED NOT NULL,
    sprite_id INT UNSIGNED NOT NULL,
    sale_date DATE NOT NULL,
    sold BIGINT UNSIGNED NOT NULL,
    total_price BIGINT UNSIGNED NOT NULL,
    PRIMARY KEY (item_type,sprite_id,sale_date),
    INDEX ix_marketplace_daily_date (sale_date,item_type,sprite_id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS marketplace_operations (
    operation_ref CHAR(36) NOT NULL PRIMARY KEY,
    user_id INT NOT NULL,
    kind VARCHAR(12) NOT NULL,
    created_at DATETIME(6) NOT NULL,
    KEY user_operations (user_id,created_at)
) ENGINE=InnoDB;