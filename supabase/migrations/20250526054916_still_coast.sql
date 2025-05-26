/*
  # Fix users table RLS policies

  1. Changes
    - Enable RLS on users table
    - Add policies for:
      - Insert: Allow public to insert new users
      - Select: Allow users to view their own data
      - Update: Allow users to update their own data
*/

-- Enable RLS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Allow public registration
CREATE POLICY "Allow public registration" ON users
  FOR INSERT
  TO public
  WITH CHECK (true);

-- Allow users to view their own data
CREATE POLICY "Users can view own data" ON users
  FOR SELECT
  TO public
  USING (auth.uid() = id);

-- Allow users to update their own data
CREATE POLICY "Users can update own data" ON users
  FOR UPDATE
  TO public
  USING (auth.uid() = id)
  WITH CHECK (auth.uid() = id);