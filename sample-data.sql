-- Sample templates for testing
INSERT INTO "Templates" ("Id", "Key", "Subject", "Body", "CreatedAt")
VALUES 
    ('01234567-89ab-cdef-0123-456789abcdef', 'welcome', 'Welcome {{customer_name}}!', 
     'Hello {{customer_name}}, welcome to our service! Your account has been created successfully.', 
     NOW()),
    ('01234567-89ab-cdef-0123-456789abcde0', 'order_confirmation', 'Order #{{order_id}} Confirmed', 
     'Hi {{customer_name}}, your order #{{order_id}} for ${{total}} has been confirmed. Expected delivery: {{delivery_date}}.', 
     NOW()),
    ('01234567-89ab-cdef-0123-456789abcde1', 'password_reset', 'Password Reset Request', 
     'Hello {{customer_name}}, you requested a password reset. Click here to reset: {{reset_link}}', 
     NOW());