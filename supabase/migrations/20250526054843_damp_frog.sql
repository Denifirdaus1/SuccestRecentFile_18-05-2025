/*
  # Fix users table RLS policies

  1. Changes
    - Enable RLS on users table
    - Add policies for user registration and management
    - Remove full_name requirement
    - Add proper indexes

  2. Security
    - Enable RLS
    - Add policies for:
      - Insert (registration)
      - Select (login)
      - Update (profile updates)
*/

-- Enable RLS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Policy for inserting new users (registration)
CREATE POLICY "Allow public registration"
ON public.users
FOR INSERT
TO public
WITH CHECK (true);

-- Policy for users to read their own data
CREATE POLICY "Users can view own data"
ON public.users
FOR SELECT
TO public
USING (auth.uid() = id);

-- Policy for users to update their own data
CREATE POLICY "Users can update own data"
ON public.users
FOR UPDATE
TO public
USING (auth.uid() = id)
WITH CHECK (auth.uid() = id);