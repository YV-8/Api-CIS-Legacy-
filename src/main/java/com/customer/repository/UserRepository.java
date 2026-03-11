package com.customer.repository;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import com.customer.models.User;

@Repository
public interface UserRepository extends JpaRepository<User, String> {
    
}
