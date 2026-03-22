package com.user.management.repository;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.Optional;
import org.springframework.stereotype.Repository;

import com.user.management.models.User;

@Repository
public interface UserRepository extends JpaRepository<User, String> {
}
