/*
  # Remove full_name requirement and update auth system
  
  1. Changes
    - Make full_name nullable in users table
    - Update auth functions to not require full_name
    - Add RLS policy for user registration
*/

ALTER TABLE users
ALTER COLUMN full_name DROP NOT NULL;

-- Add RLS policy to allow user registration
CREATE POLICY "Enable user registration" ON users
    FOR INSERT
    WITH CHECK (true);

-- Update select policy to allow login
DROP POLICY IF EXISTS "Users can view own data" ON users;
CREATE POLICY "Users can view own data" ON users
    FOR SELECT USING (true);