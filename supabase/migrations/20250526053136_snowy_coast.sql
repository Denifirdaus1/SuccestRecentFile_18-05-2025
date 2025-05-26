/*
  # Remove full_name requirement and update auth system
  
  1. Changes
    - Make full_name nullable in users table
    - Update auth functions to not require full_name
*/

ALTER TABLE users
ALTER COLUMN full_name DROP NOT NULL;